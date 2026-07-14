using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.ImageRedactor
{
    public partial class ImageColorPickerDialog : Window
    {
        public ImageColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            SelectedColor = initialColor;
            SetSliders(initialColor);
            UpdatePreview();
        }

        public Color SelectedColor { get; private set; }

        private void SetSliders(Color color)
        {
            AlphaSlider.Value = color.A;
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            SelectedColor = Color.FromArgb(
                (byte)AlphaSlider.Value,
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value);

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            AlphaTextBox.Text = ((int)AlphaSlider.Value).ToString();
            RedTextBox.Text = ((int)RedSlider.Value).ToString();
            GreenTextBox.Text = ((int)GreenSlider.Value).ToString();
            BlueTextBox.Text = ((int)BlueSlider.Value).ToString();
            PreviewBorder.Background = new SolidColorBrush(SelectedColor);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
