#ifndef vtkCloudCompareEDLPass_h
#define vtkCloudCompareEDLPass_h

#include "vtkDepthImageProcessingPass.h"
#include "vtkOpenGLHelper.h"

VTK_ABI_NAMESPACE_BEGIN
class vtkOpenGLFramebufferObject;
class vtkOpenGLRenderWindow;
class vtkTextureObject;

class vtkCloudCompareEDLPass : public vtkDepthImageProcessingPass
{
public:
    static vtkCloudCompareEDLPass* New();
    vtkTypeMacro(vtkCloudCompareEDLPass, vtkDepthImageProcessingPass);
    void PrintSelf(ostream& os, vtkIndent indent) override;

    void Render(const vtkRenderState* s) override;
    void ReleaseGraphicsResources(vtkWindow* w) override;

    vtkSetMacro(Strength, float);
    vtkGetMacro(Strength, float);

    vtkSetMacro(RadiusPerspective, float);
    vtkGetMacro(RadiusPerspective, float);

    vtkSetMacro(RadiusOrthographic, float);
    vtkGetMacro(RadiusOrthographic, float);

    vtkSetVector3Macro(LightDir, float);
    vtkGetVector3Macro(LightDir, float);

protected:
    vtkCloudCompareEDLPass();
    ~vtkCloudCompareEDLPass() override;

    void InitializeFramebuffers(vtkRenderState& s);
    void InitializeShaders(vtkOpenGLRenderWindow* renWin);

    bool Shade(vtkRenderState& s,
               vtkOpenGLRenderWindow* renWin,
               int scaleFactor,
               vtkOpenGLFramebufferObject* fbo,
               vtkTextureObject* targetTex,
               float neighborDistance,
               int perspectiveMode);

    bool BilateralBlur(vtkOpenGLRenderWindow* renWin,
                       vtkTextureObject* sourceTex,
                       vtkTextureObject* targetTex,
                       vtkOpenGLFramebufferObject* targetFbo,
                       int width,
                       int height,
                       int perspectiveMode);

    bool Compose(const vtkRenderState* s, vtkOpenGLRenderWindow* renWin);

    vtkOpenGLFramebufferObject* ProjectionFBO;
    vtkTextureObject* ProjectionColorTexture;
    vtkTextureObject* ProjectionDepthTexture;

    vtkOpenGLFramebufferObject* FBO1;
    vtkTextureObject* ShadeTex1;

    vtkOpenGLFramebufferObject* FBO2;
    vtkTextureObject* ShadeTex2;
    vtkOpenGLFramebufferObject* BlurFBO2;
    vtkTextureObject* BlurTex2;

    vtkOpenGLFramebufferObject* FBO4;
    vtkTextureObject* ShadeTex4;
    vtkOpenGLFramebufferObject* BlurFBO4;
    vtkTextureObject* BlurTex4;

    vtkOpenGLHelper ShadeProgram;
    vtkOpenGLHelper ComposeProgram;
    vtkOpenGLHelper BilateralProgram;

    float Strength;
    float RadiusPerspective;
    float RadiusOrthographic;
    float LightDir[3];
    float Neighbours[8][4];

    double Znear;
    double Zfar;

private:
    vtkCloudCompareEDLPass(const vtkCloudCompareEDLPass&) = delete;
    void operator=(const vtkCloudCompareEDLPass&) = delete;
};

VTK_ABI_NAMESPACE_END

#endif
