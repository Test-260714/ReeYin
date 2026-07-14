#include "pch.h"

#include "vtkCloudCompareEDLPass.h"

#include "vtkCamera.h"
#include "vtkMath.h"
#include "vtkObjectFactory.h"
#include "vtkOpenGLFramebufferObject.h"
#include "vtkOpenGLRenderWindow.h"
#include "vtkOpenGLShaderCache.h"
#include "vtkOpenGLState.h"
#include "vtkRenderState.h"
#include "vtkRenderer.h"
#include "vtkShaderProgram.h"
#include "vtkTextureObject.h"
#include "vtk_glad.h"

#include <algorithm>
#include <cmath>

vtkStandardNewMacro(vtkCloudCompareEDLPass);

namespace
{
constexpr char EDLVertexShaderSource[] = R"(//VTK::System::Dec
in vec4 vertexMC;
in vec2 tcoordMC;
out vec2 tcoordVC;

void main()
{
  tcoordVC = tcoordMC;
  gl_Position = vertexMC;
}
)";

constexpr char EDLShadeShaderSource[] = R"(//VTK::System::Dec
//VTK::Output::Dec
in vec2 tcoordVC;
uniform sampler2D s1_color;
uniform sampler2D s2_depth;
uniform float Pix_scale;
uniform vec4 Neigh_pos_2D[8];
uniform float Exp_scale;
uniform float Zm;
uniform float ZM;
uniform float Sx;
uniform float Sy;
uniform float Dist_to_neighbor_pix;
uniform int PerspectiveMode;
uniform vec3 Light_dir;

float fixDepth(float depth)
{
  if (PerspectiveMode == 1)
  {
    depth = (2.0 * ZM * Zm) / ((ZM + Zm) - (2.0 * depth - 1.0) * (ZM - Zm));
    depth = (depth - Zm) / (ZM - Zm);
  }
  return clamp(1.0 - depth, 0.0, 1.0);
}

float computeObscurance(float depth, float scale)
{
  vec4 P = vec4(Light_dir.xyz, -dot(Light_dir.xyz, vec3(0.0, 0.0, depth)));
  float sum = 0.0;
  for (int c = 0; c < 8; ++c)
  {
    vec2 nRel = scale * Dist_to_neighbor_pix / vec2(Sx, Sy) * Neigh_pos_2D[c].xy;
    vec2 nAbs = tcoordVC + nRel;
    float zn = fixDepth(texture(s2_depth, nAbs).r);
    float znp = dot(vec4(nRel, zn, 1.0), P);
    sum += max(0.0, znp) / scale;
  }
  return sum;
}

void main()
{
  vec3 rgb = texture(s1_color, tcoordVC).rgb;
  float depthRaw = texture(s2_depth, tcoordVC).r;
  float depth = fixDepth(depthRaw);
  if (depth > 0.001)
  {
    float f = computeObscurance(depth, Pix_scale);
    f = exp(-Exp_scale * f);
    gl_FragData[0] = vec4(f * rgb, depthRaw);
  }
  else
  {
    gl_FragData[0] = vec4(rgb, depthRaw);
  }
}
)";

constexpr char EDLComposeShaderSource[] = R"(//VTK::System::Dec
//VTK::Output::Dec
in vec2 tcoordVC;
uniform sampler2D s2_I1;
uniform sampler2D s2_I2;
uniform sampler2D s2_I4;
uniform sampler2D s2_D;
uniform float A0;
uniform float A1;
uniform float A2;

void main()
{
  float d = texture(s2_D, tcoordVC).r;
  vec4 c1 = texture(s2_I1, tcoordVC);
  vec3 C1 = c1.rgb;
  if (d > 0.999)
  {
    gl_FragData[0] = vec4(C1, 1.0);
    gl_FragDepth = d;
    return;
  }

  vec3 C2 = texture(s2_I2, tcoordVC).rgb;
  vec3 C4 = texture(s2_I4, tcoordVC).rgb;
  vec3 C = (A0 * C1 + A1 * C2 + A2 * C4) / (A0 + A1 + A2);
  gl_FragData[0] = vec4(C, 1.0);
  gl_FragDepth = c1.a;
}
)";

constexpr char EDLBilateralShaderSource[] = R"(//VTK::System::Dec
//VTK::Output::Dec
in vec2 tcoordVC;
uniform sampler2D s1_color;
uniform sampler2D s2_depth;
uniform vec2 TexelSize;
uniform float Sigma;
uniform float SigmaZ;
uniform int PerspectiveMode;
uniform float Zm;
uniform float ZM;

float fixDepth(float depth)
{
  if (PerspectiveMode == 1)
  {
    depth = (2.0 * ZM * Zm) / ((ZM + Zm) - (2.0 * depth - 1.0) * (ZM - Zm));
    depth = (depth - Zm) / (ZM - Zm);
  }
  return clamp(1.0 - depth, 0.0, 1.0);
}

void main()
{
  vec4 center = texture(s1_color, tcoordVC);
  float centerDepth = fixDepth(texture(s2_depth, tcoordVC).r);
  vec3 sum = vec3(0.0);
  float wsum = 0.0;

  for (int y = -2; y <= 2; ++y)
  {
    for (int x = -2; x <= 2; ++x)
    {
      vec2 offset = vec2(float(x), float(y)) * TexelSize;
      vec2 uv = tcoordVC + offset;
      vec4 sampleColor = texture(s1_color, uv);
      float sampleDepth = fixDepth(texture(s2_depth, uv).r);
      float spatial =
        exp(-dot(vec2(float(x), float(y)), vec2(float(x), float(y))) / (2.0 * Sigma * Sigma));
      float range = exp(
        -(sampleDepth - centerDepth) * (sampleDepth - centerDepth) / (2.0 * SigmaZ * SigmaZ));
      float w = spatial * range;
      sum += sampleColor.rgb * w;
      wsum += w;
    }
  }

  vec3 filtered = wsum > 0.0 ? sum / wsum : center.rgb;
  gl_FragData[0] = vec4(filtered, center.a);
}
)";
}

vtkCloudCompareEDLPass::vtkCloudCompareEDLPass()
{
    this->ProjectionFBO = nullptr;
    this->ProjectionColorTexture = nullptr;
    this->ProjectionDepthTexture = nullptr;

    this->FBO1 = nullptr;
    this->FBO2 = nullptr;
    this->FBO4 = nullptr;
    this->BlurFBO2 = nullptr;
    this->BlurFBO4 = nullptr;

    this->ShadeTex1 = nullptr;
    this->ShadeTex2 = nullptr;
    this->ShadeTex4 = nullptr;
    this->BlurTex2 = nullptr;
    this->BlurTex4 = nullptr;

    this->Strength = 100.0f;
    this->RadiusPerspective = 3.0f;
    this->RadiusOrthographic = 1.2f;

    this->LightDir[0] = 0.0f;
    this->LightDir[1] = 0.0f;
    this->LightDir[2] = 1.0f;

    this->Znear = 0.1;
    this->Zfar = 1000.0;

    for (int c = 0; c < 8; ++c)
    {
        const double angle = 2.0 * vtkMath::Pi() * static_cast<double>(c) / 8.0;
        this->Neighbours[c][0] = static_cast<float>(std::cos(angle));
        this->Neighbours[c][1] = static_cast<float>(std::sin(angle));
        this->Neighbours[c][2] = 0.0f;
        this->Neighbours[c][3] = 0.0f;
    }
}

vtkCloudCompareEDLPass::~vtkCloudCompareEDLPass()
{
    if (this->ProjectionFBO)
    {
        this->ProjectionFBO->Delete();
    }
    if (this->ProjectionColorTexture)
    {
        this->ProjectionColorTexture->Delete();
    }
    if (this->ProjectionDepthTexture)
    {
        this->ProjectionDepthTexture->Delete();
    }

    if (this->FBO1)
    {
        this->FBO1->Delete();
    }
    if (this->FBO2)
    {
        this->FBO2->Delete();
    }
    if (this->FBO4)
    {
        this->FBO4->Delete();
    }
    if (this->BlurFBO2)
    {
        this->BlurFBO2->Delete();
    }
    if (this->BlurFBO4)
    {
        this->BlurFBO4->Delete();
    }

    if (this->ShadeTex1)
    {
        this->ShadeTex1->Delete();
    }
    if (this->ShadeTex2)
    {
        this->ShadeTex2->Delete();
    }
    if (this->ShadeTex4)
    {
        this->ShadeTex4->Delete();
    }
    if (this->BlurTex2)
    {
        this->BlurTex2->Delete();
    }
    if (this->BlurTex4)
    {
        this->BlurTex4->Delete();
    }
}

void vtkCloudCompareEDLPass::PrintSelf(ostream& os, vtkIndent indent)
{
    this->Superclass::PrintSelf(os, indent);
    os << indent << "Strength: " << this->Strength << "\n";
    os << indent << "RadiusPerspective: " << this->RadiusPerspective << "\n";
    os << indent << "RadiusOrthographic: " << this->RadiusOrthographic << "\n";
    os << indent << "LightDir: " << this->LightDir[0] << ", " << this->LightDir[1] << ", " << this->LightDir[2] << "\n";
}

void vtkCloudCompareEDLPass::InitializeFramebuffers(vtkRenderState& s)
{
    vtkRenderer* renderer = s.GetRenderer();
    vtkOpenGLRenderWindow* renWin = vtkOpenGLRenderWindow::SafeDownCast(renderer->GetRenderWindow());

    if (!this->ProjectionFBO)
    {
        this->ProjectionFBO = vtkOpenGLFramebufferObject::New();
        this->ProjectionFBO->SetContext(renWin);
    }

    auto createTex = [&](vtkTextureObject*& tex, int width, int height, bool isDepth)
    {
        const int w = std::max(1, width);
        const int h = std::max(1, height);

        if (!tex)
        {
            tex = vtkTextureObject::New();
            tex->SetContext(renWin);
        }

        if (tex->GetWidth() != static_cast<unsigned int>(w) || tex->GetHeight() != static_cast<unsigned int>(h))
        {
            if (isDepth)
            {
                tex->AllocateDepth(w, h, vtkTextureObject::Float32);
            }
            else
            {
                tex->Create2D(w, h, 4, VTK_FLOAT, false);
            }
        }
    };

    createTex(this->ProjectionColorTexture, this->W, this->H, false);
    createTex(this->ProjectionDepthTexture, this->W, this->H, true);
    createTex(this->ShadeTex1, this->W, this->H, false);
    createTex(this->ShadeTex2, this->W / 2, this->H / 2, false);
    createTex(this->ShadeTex4, this->W / 4, this->H / 4, false);
    createTex(this->BlurTex2, this->W / 2, this->H / 2, false);
    createTex(this->BlurTex4, this->W / 4, this->H / 4, false);

    if (!this->FBO1)
    {
        this->FBO1 = vtkOpenGLFramebufferObject::New();
        this->FBO1->SetContext(renWin);
    }
    if (!this->FBO2)
    {
        this->FBO2 = vtkOpenGLFramebufferObject::New();
        this->FBO2->SetContext(renWin);
    }
    if (!this->FBO4)
    {
        this->FBO4 = vtkOpenGLFramebufferObject::New();
        this->FBO4->SetContext(renWin);
    }
    if (!this->BlurFBO2)
    {
        this->BlurFBO2 = vtkOpenGLFramebufferObject::New();
        this->BlurFBO2->SetContext(renWin);
    }
    if (!this->BlurFBO4)
    {
        this->BlurFBO4 = vtkOpenGLFramebufferObject::New();
        this->BlurFBO4->SetContext(renWin);
    }
}

void vtkCloudCompareEDLPass::InitializeShaders(vtkOpenGLRenderWindow* renWin)
{
    if (this->ShadeProgram.Program && this->ComposeProgram.Program && this->BilateralProgram.Program)
    {
        return;
    }

    if (!this->ShadeProgram.Program)
    {
        this->ShadeProgram.Program =
            renWin->GetShaderCache()->ReadyShaderProgram(EDLVertexShaderSource, EDLShadeShaderSource, "");
    }

    if (!this->ComposeProgram.Program)
    {
        this->ComposeProgram.Program =
            renWin->GetShaderCache()->ReadyShaderProgram(EDLVertexShaderSource, EDLComposeShaderSource, "");
    }

    if (!this->BilateralProgram.Program)
    {
        this->BilateralProgram.Program =
            renWin->GetShaderCache()->ReadyShaderProgram(EDLVertexShaderSource, EDLBilateralShaderSource, "");
    }

    if (!this->ShadeProgram.Program || !this->ComposeProgram.Program || !this->BilateralProgram.Program)
    {
        static bool warnedCompileFailure = false;
        if (!warnedCompileFailure)
        {
            warnedCompileFailure = true;
            vtkWarningMacro(<< "Failed to compile one or more EDL shaders. Falling back to delegate rendering.");
        }
    }
}

bool vtkCloudCompareEDLPass::Shade(vtkRenderState& s,
                                   vtkOpenGLRenderWindow* renWin,
                                   int scaleFactor,
                                   vtkOpenGLFramebufferObject* fbo,
                                   vtkTextureObject* targetTex,
                                   float neighborDistance,
                                   int perspectiveMode)
{
    if (!fbo || !targetTex || !this->ShadeProgram.Program)
    {
        return false;
    }

    const int w = std::max(1, this->W / scaleFactor);
    const int h = std::max(1, this->H / scaleFactor);

    renWin->GetState()->PushFramebufferBindings();

    fbo->Bind();
    fbo->AddColorAttachment(0, targetTex);
    fbo->ActivateDrawBuffer(0);
    fbo->Start(w, h);

    renWin->GetShaderCache()->ReadyShaderProgram(this->ShadeProgram.Program);
    vtkShaderProgram* program = this->ShadeProgram.Program;

    this->ProjectionColorTexture->Activate();
    this->ProjectionDepthTexture->Activate();

    program->SetUniformi("s1_color", this->ProjectionColorTexture->GetTextureUnit());
    program->SetUniformi("s2_depth", this->ProjectionDepthTexture->GetTextureUnit());
    program->SetUniformf("Pix_scale", static_cast<float>(scaleFactor));
    program->SetUniform4fv("Neigh_pos_2D", 8, this->Neighbours);
    program->SetUniformf("Exp_scale", this->Strength);
    program->SetUniformf("Zm", static_cast<float>(this->Znear));
    program->SetUniformf("ZM", static_cast<float>(this->Zfar));
    program->SetUniformf("Sx", static_cast<float>(this->W));
    program->SetUniformf("Sy", static_cast<float>(this->H));
    program->SetUniformf("Dist_to_neighbor_pix", neighborDistance);
    program->SetUniformi("PerspectiveMode", perspectiveMode);
    program->SetUniform3f("Light_dir", this->LightDir);

    fbo->RenderQuad(0, w - 1, 0, h - 1, program, this->ShadeProgram.VAO);

    this->ProjectionDepthTexture->Deactivate();
    this->ProjectionColorTexture->Deactivate();

    renWin->GetState()->PopFramebufferBindings();
    return true;
}

bool vtkCloudCompareEDLPass::BilateralBlur(vtkOpenGLRenderWindow* renWin,
                                           vtkTextureObject* sourceTex,
                                           vtkTextureObject* targetTex,
                                           vtkOpenGLFramebufferObject* targetFbo,
                                           int width,
                                           int height,
                                           int perspectiveMode)
{
    if (!sourceTex || !targetTex || !targetFbo || !this->BilateralProgram.Program)
    {
        return false;
    }

    const int w = std::max(1, width);
    const int h = std::max(1, height);

    renWin->GetState()->PushFramebufferBindings();

    targetFbo->Bind();
    targetFbo->AddColorAttachment(0, targetTex);
    targetFbo->ActivateDrawBuffer(0);
    targetFbo->Start(w, h);

    renWin->GetShaderCache()->ReadyShaderProgram(this->BilateralProgram.Program);
    vtkShaderProgram* program = this->BilateralProgram.Program;

    sourceTex->Activate();
    this->ProjectionDepthTexture->Activate();

    program->SetUniformi("s1_color", sourceTex->GetTextureUnit());
    program->SetUniformi("s2_depth", this->ProjectionDepthTexture->GetTextureUnit());
    const float texelSize[2] = { 1.0f / static_cast<float>(w), 1.0f / static_cast<float>(h) };
    program->SetUniform2f("TexelSize", texelSize);
    program->SetUniformf("Sigma", 2.0f);
    program->SetUniformf("SigmaZ", 0.4f);
    program->SetUniformi("PerspectiveMode", perspectiveMode);
    program->SetUniformf("Zm", static_cast<float>(this->Znear));
    program->SetUniformf("ZM", static_cast<float>(this->Zfar));

    targetFbo->RenderQuad(0, w - 1, 0, h - 1, program, this->BilateralProgram.VAO);

    this->ProjectionDepthTexture->Deactivate();
    sourceTex->Deactivate();

    renWin->GetState()->PopFramebufferBindings();
    return true;
}

bool vtkCloudCompareEDLPass::Compose(const vtkRenderState* s, vtkOpenGLRenderWindow* renWin)
{
    if (!this->ComposeProgram.Program)
    {
        return false;
    }

    vtkTextureObject* tex2 = this->BlurTex2 ? this->BlurTex2 : this->ShadeTex2;
    vtkTextureObject* tex4 = this->BlurTex4 ? this->BlurTex4 : this->ShadeTex4;

    renWin->GetShaderCache()->ReadyShaderProgram(this->ComposeProgram.Program);
    vtkShaderProgram* program = this->ComposeProgram.Program;

    this->ShadeTex1->Activate();
    tex2->Activate();
    tex4->Activate();
    this->ProjectionDepthTexture->Activate();

    program->SetUniformi("s2_I1", this->ShadeTex1->GetTextureUnit());
    program->SetUniformi("s2_I2", tex2->GetTextureUnit());
    program->SetUniformi("s2_I4", tex4->GetTextureUnit());
    program->SetUniformi("s2_D", this->ProjectionDepthTexture->GetTextureUnit());
    program->SetUniformf("A0", 1.0f);
    program->SetUniformf("A1", 0.5f);
    program->SetUniformf("A2", 0.25f);

    auto state = renWin->GetState();
    vtkOpenGLState::ScopedglEnableDisable savedBlend(state, GL_BLEND);
    vtkOpenGLState::ScopedglEnableDisable savedDepthTest(state, GL_DEPTH_TEST);
    vtkOpenGLState::ScopedglDepthFunc savedDepthFunc(state);
    state->vtkglDisable(GL_BLEND);
    state->vtkglEnable(GL_DEPTH_TEST);
    state->vtkglDepthFunc(GL_ALWAYS);

    this->ShadeTex1->CopyToFrameBuffer(0,
                                       0,
                                       this->Width - 1,
                                       this->Height - 1,
                                       0,
                                       0,
                                       this->Width,
                                       this->Height,
                                       program,
                                       this->ComposeProgram.VAO);

    this->ShadeTex1->Deactivate();
    tex2->Deactivate();
    tex4->Deactivate();
    this->ProjectionDepthTexture->Deactivate();

    return true;
}

void vtkCloudCompareEDLPass::Render(const vtkRenderState* s)
{
    this->ReadWindowSize(s);
    this->W = this->Width;
    this->H = this->Height;

    vtkRenderer* renderer = s->GetRenderer();
    vtkOpenGLRenderWindow* renWin = vtkOpenGLRenderWindow::SafeDownCast(renderer->GetRenderWindow());

    this->InitializeFramebuffers(const_cast<vtkRenderState&>(*s));
    this->InitializeShaders(renWin);
    if (!this->ShadeProgram.Program || !this->ComposeProgram.Program || !this->BilateralProgram.Program)
    {
        if (this->DelegatePass)
        {
            this->DelegatePass->Render(s);
        }
        else
        {
            static bool warnedMissingDelegate = false;
            if (!warnedMissingDelegate)
            {
                warnedMissingDelegate = true;
                vtkWarningMacro(<< "EDL shaders are unavailable and no delegate pass is configured.");
            }
        }
        return;
    }

    vtkCamera* camera = renderer->GetActiveCamera();
    camera->GetClippingRange(this->Znear, this->Zfar);

    if (this->Zfar <= this->Znear)
    {
        this->Zfar = this->Znear + 1.0;
    }

    int perspectiveMode = camera->GetParallelProjection() ? 0 : 1;
    const float neighborDistance = perspectiveMode ? this->RadiusPerspective : this->RadiusOrthographic;

    renWin->GetState()->PushFramebufferBindings();
    this->ProjectionFBO->Bind();
    this->RenderDelegate(s,
                         this->Width,
                         this->Height,
                         this->W,
                         this->H,
                         this->ProjectionFBO,
                         this->ProjectionColorTexture,
                         this->ProjectionDepthTexture);
    renWin->GetState()->PopFramebufferBindings();

    this->Shade(const_cast<vtkRenderState&>(*s), renWin, 1, this->FBO1, this->ShadeTex1, neighborDistance, perspectiveMode);
    this->Shade(const_cast<vtkRenderState&>(*s), renWin, 2, this->FBO2, this->ShadeTex2, neighborDistance, perspectiveMode);
    this->Shade(const_cast<vtkRenderState&>(*s), renWin, 4, this->FBO4, this->ShadeTex4, neighborDistance, perspectiveMode);
    this->BilateralBlur(renWin,
                        this->ShadeTex2,
                        this->BlurTex2,
                        this->BlurFBO2,
                        this->W / 2,
                        this->H / 2,
                        perspectiveMode);
    this->BilateralBlur(renWin,
                        this->ShadeTex4,
                        this->BlurTex4,
                        this->BlurFBO4,
                        this->W / 4,
                        this->H / 4,
                        perspectiveMode);

    if (s->GetFrameBuffer())
    {
        vtkOpenGLFramebufferObject::SafeDownCast(s->GetFrameBuffer())->Bind();
    }

    this->Compose(s, renWin);
}

void vtkCloudCompareEDLPass::ReleaseGraphicsResources(vtkWindow* w)
{
    this->ShadeProgram.ReleaseGraphicsResources(w);
    this->ComposeProgram.ReleaseGraphicsResources(w);
    this->BilateralProgram.ReleaseGraphicsResources(w);
    this->Superclass::ReleaseGraphicsResources(w);
}
