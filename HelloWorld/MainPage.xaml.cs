using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Shapes;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.UI.Input;
using HelloWorld.Utils;
using System.Diagnostics;
using Windows.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HelloWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Stack<InkStroke> undoStack { get; set; }

        private List<Color> colorArray = null;

        InkAnalyzer analyzerShape = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesShape = null;
        InkAnalysisResult resultShape = null;
        private Random rng = new Random();
        List<Rectangle> postits = null;
        public CommentModel comments;

        public MainPage()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputDeviceTypes =

                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                // Uncomment the line below if you want to draw with touch
                // When commented out, long touch to create comment
                // Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            //Multiple pen input, breaks undo!
            //inkCanvas.InkPresenter.ActivateCustomDrying();
            //inkCanvas.InkPresenter.SetPredefinedConfiguration(InkPresenterPredefinedConfiguration.SimpleMultiplePointer);

            undoStack = new Stack<InkStroke>();
            postits = new List<Rectangle>();
            comments = new CommentModel();

            colorArray = new List<Color>();

            colorArray.Add((Windows.UI.Colors.Goldenrod));
            colorArray.Add((Windows.UI.Colors.LightSkyBlue));
            colorArray.Add((Windows.UI.Colors.Plum));
            colorArray.Add((Windows.UI.Colors.PaleGreen));

            

            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += ClearStack;
            
            // enable adding comments with right click (how in hub??)
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += OtherMakePopup;

            inkCanvas.RightTapped += TouchMakePopup;

        }


        public Rectangle DrawRectangle(Comment comment)
        {
            Rectangle rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(comment.fill);
            rectangle.Width = comment.width;
            rectangle.Height = comment.height;
            rectangle.Opacity = comment.opacity;
            Canvas.SetLeft(rectangle, comment.left);
            Canvas.SetTop(rectangle, comment.top);

            var rotation = new RotateTransform();
            rotation.Angle = comment.angle;
            rectangle.RenderTransform = rotation;

            // Add flyout
            var flyout = new Flyout();
            Style flyoutStyle = new Style();
            flyoutStyle.TargetType = typeof(FlyoutPresenter);
            flyoutStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(comment.fill)));
            flyout.FlyoutPresenterStyle = flyoutStyle;

            // Add delete button
            Button deleteButton = new Button();
            deleteButton.Content = new SymbolIcon(Symbol.Delete);
            deleteButton.Click += async delegate (object e, RoutedEventArgs evt)
            {
                canvas.Children.Remove(rectangle);
                comments.Remove(comment);
            };

            // Add canvas
            InkCanvas flyoutInkCanvas = new InkCanvas();
            flyoutInkCanvas.Width = 250;
            flyoutInkCanvas.Height = 250;
            flyoutInkCanvas.InkPresenter.InputDeviceTypes = 
              Windows.UI.Core.CoreInputDeviceTypes.Mouse |
              Windows.UI.Core.CoreInputDeviceTypes.Touch |
              Windows.UI.Core.CoreInputDeviceTypes.Pen;
        
            InkToolbar flyoutInkToolbar = new InkToolbar();
            flyoutInkToolbar.TargetInkCanvas = flyoutInkCanvas;

            // Add panels
            StackPanel stackPanel = new StackPanel();
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            StackPanel rightAlignment = new StackPanel();
            rightAlignment.HorizontalAlignment = HorizontalAlignment.Right;
            rightAlignment.Children.Add(deleteButton);

            stackPanel.Children.Add(rightAlignment);
            stackPanel.Children.Add(flyoutInkCanvas);
            stackPanel.Children.Add(flyoutInkToolbar);

            // Additional settings
            flyout.Content = stackPanel;
            flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;
            rectangle.ContextFlyout = flyout;
            
            canvas.Children.Add(rectangle);
            return rectangle;
        }


        public void makeComment(double x, double y) 
        {
            
            var newComment = comments.CreateComment(x, y);
            var rect = DrawRectangle(newComment);
            rect.ContextFlyout.ShowAt(rect);
            

        }

        private void TouchMakePopup(object sender, RightTappedRoutedEventArgs args)
        {
            Point point = args.GetPosition(inkCanvas);
            makeComment(point.X, point.Y);

            
        }

        private void OtherMakePopup(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            PointerPoint point = args.CurrentPoint;

            makeComment(point.Position.X, point.Position.Y);
        }


        public void saveAll(object sender, RoutedEventArgs e)
        {
            Save.SaveComments(canvas, comments);
        }

        public async void loadAll(object sender, RoutedEventArgs e)
        {
            CommentModel loadedComments = await Save.LoadComments(canvas);
            if (loadedComments != null)
            {
                comments = loadedComments; // update model
                canvas.Children.Clear(); // probably better way than this...
                foreach(Comment c in loadedComments.GetComments())
                {
                    DrawRectangle(c);
                }
            }
        }


        private void ClearStack(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            // clear the stack if a new stroke has been added
            undoStack.Clear();
        }

        private void saveInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            Save.SaveInk(inkCanvas);
        }

        private void loadInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            Save.LoadInk(inkCanvas);
        }

        private async void recogniseShape_ClickAsync(object sender, RoutedEventArgs e)
        {
            strokesShape = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokesShape.Count > 0)
            {
                analyzerShape.AddDataForStrokes(strokesShape);
                resultShape = await analyzerShape.AnalyzeAsync();

                if (resultShape.Status == InkAnalysisStatus.Updated)
                {
                    var drawings = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                    foreach (var drawing in drawings)
                    {
                        var shape = (InkAnalysisInkDrawing)drawing;
                        if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
                        {
                            // Catch and process unsupported shapes (lines and so on) here.
                        }
                        else
                        {
                            // Process recognized shapes here.
                            if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                            {
                                DrawEllipse(shape);
                            }
                            else
                            {
                                DrawPolygon(shape);
                            }
                            foreach (var strokeId in shape.GetStrokeIds())
                            {
                                var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                stroke.Selected = true;
                            }
                        }
                        analyzerShape.RemoveDataForStrokes(shape.GetStrokeIds());
                    }
                    inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                }
            }
        }

        private void backToMenu(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Home));
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            //Source: http://edi.wang/post/2017/7/25/uwp-ink-undo-redo
            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count > 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                undoStack.Push(strokes[strokes.Count - 1]);
                inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }

        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                var stroke = undoStack.Pop();

                var strokeBuilder = new InkStrokeBuilder();
                strokeBuilder.SetDefaultDrawingAttributes(stroke.DrawingAttributes);
                System.Numerics.Matrix3x2 matrix = stroke.PointTransform;
                IReadOnlyList<InkPoint> inkPoints = stroke.GetInkPoints();
                InkStroke inkStroke = strokeBuilder.CreateStrokeFromInkPoints(inkPoints, matrix);
                inkCanvas.InkPresenter.StrokeContainer.AddStroke(inkStroke);
            }
        }

        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Ellipse ellipse = new Ellipse();
            ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
                 (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
            ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
                 (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

            var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = ellipse.Width / 2.0;
            rotateTransform.CenterY = ellipse.Height / 2.0;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
            translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 2;
            canvas.Children.Add(ellipse);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 2;
            canvas.Children.Add(polygon);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is IRandomAccessStream)
            {
                var stream = (IRandomAccessStream)e.Parameter;
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
            base.OnNavigatedTo(e);
        }


    }
}
