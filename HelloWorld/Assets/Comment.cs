using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace HelloWorld.Assets
{
    [DataContract]
    class Comment
    {
        public DependencyProperty BackgroundProperty { get; private set; }
        [DataMember]
        public Rectangle rectangle { get; set; }

        [DataMember]
        int width { get; set; }

        [DataMember]
        int height { get; set; }

        [DataMember]
        int posX { get; set; }

        [DataMember]
        int posY { get; set; }

        [DataMember]
        RotateTransform rotation { get; set; }

        [DataMember]
        SolidColorBrush brush { get; set; }

        [DataMember]
        InkStrokeContainer strokes { get; set; }

        public Comment(double x, double y)
        {
            rectangle = new Rectangle();
            rectangle.Width = 25;
            rectangle.Height = 25;
            rectangle.Opacity = 0.8;
            var rotation = new RotateTransform();
            rotation.Angle = -30;
            rectangle.RenderTransform = rotation;
            Flyout flyout = new Flyout();

            FlyoutPresenter fp = new FlyoutPresenter();

            //Style fps = new Style();
            //fps.TargetType = typeof(FlyoutPresenter);
            //fps.Setters.Add(new Setter(BackgroundProperty, colorDecision));
            //fp.Style = fps;
            //flyout.FlyoutPresenterStyle = fps;

            Button deleteButton = new Button();

            SymbolIcon deleteSymbol = new SymbolIcon();
            deleteSymbol.Symbol = Symbol.Delete;
            deleteButton.Content = deleteSymbol;
            deleteButton.Click += async delegate (object e, RoutedEventArgs evt)
            {
                //canvas.Children.Remove(rectangle);
                //postits.Remove(rectangle);
            };

            InkCanvas ic = new InkCanvas();
            ic.Width = 250;
            ic.Height = 250;

            InkToolbar it = new InkToolbar();
            it.TargetInkCanvas = ic;



            ic.InkPresenter.InputDeviceTypes =
              Windows.UI.Core.CoreInputDeviceTypes.Mouse |
              Windows.UI.Core.CoreInputDeviceTypes.Touch |
              Windows.UI.Core.CoreInputDeviceTypes.Pen;

            StackPanel sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.HorizontalAlignment = HorizontalAlignment.Center;

            StackPanel rightAlign = new StackPanel();
            rightAlign.HorizontalAlignment = HorizontalAlignment.Right;
            rightAlign.Children.Add(deleteButton);
            sp.Children.Add(rightAlign);
            sp.Children.Add(ic);
            sp.Children.Add(it);

            flyout.Content = sp;
            flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;
            rectangle.ContextFlyout = flyout;

        }

    }
}
