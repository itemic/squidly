﻿using System;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HelloWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Stack<InkStroke> undoStack { get; set; }

        InkAnalyzer analyzerShape = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesShape = null;
        InkAnalysisResult resultShape = null;

        public MainPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes =

                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                // Uncomment the line below if you want to draw with touch
                // When commented out, long touch to create comment
                Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            Canvas2.InkPresenter.InputDeviceTypes =
               Windows.UI.Core.CoreInputDeviceTypes.Mouse |
               Windows.UI.Core.CoreInputDeviceTypes.Touch |
               Windows.UI.Core.CoreInputDeviceTypes.Pen;

            undoStack = new Stack<InkStroke>();

            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += ClearStack;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += OtherMakePopup;
            inkCanvas.RightTapped += new RightTappedEventHandler(CreatePopup);
        }

        private void OtherMakePopup(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            PointerPoint point = args.CurrentPoint;

            var rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.Goldenrod);
            rectangle.Width = 25;
            rectangle.Height = 25;
            rectangle.Opacity = 0.8;
            var rotation = new RotateTransform();
            rotation.Angle = -25;
            rectangle.RenderTransform = rotation;

            Canvas.SetLeft(rectangle, point.Position.X - 12.5);
            Canvas.SetTop(rectangle, point.Position.Y - 12.5);


            Flyout flyout = new Flyout();

            TextBlock tb = new TextBlock();
            tb.Text = "New Comment";

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
            sp.Children.Add(tb);
            sp.Children.Add(it);
            sp.Children.Add(ic);

            flyout.Content = sp;

            rectangle.PointerReleased += async delegate (object s, PointerRoutedEventArgs evt)
            {
                flyout.ShowAt(rectangle);
            };

            canvas.Children.Add(rectangle);

            flyout.ShowAt(rectangle);
        }

        private void CommentMode(object sender, RoutedEventArgs e)
        {
            var inputs = inkCanvas.InkPresenter.InputDeviceTypes;
            if (inputs == (Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen))
            {
               inputs = Windows.UI.Core.CoreInputDeviceTypes.None;
            } else
            {
                inputs = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen;
            }

            inkCanvas.InkPresenter.InputDeviceTypes = inputs;
        }

       
        private async void CreatePopup(object sender, RightTappedRoutedEventArgs e)
        {

            Point point = e.GetPosition(inkCanvas);

            var rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.Goldenrod);
            rectangle.Width = 25;
            rectangle.Height = 25;
            rectangle.Opacity = 0.8;
            var rotation = new RotateTransform();
            rotation.Angle = -25;
            rectangle.RenderTransform = rotation;

            Canvas.SetLeft(rectangle, point.X - 12.5);
            Canvas.SetTop(rectangle, point.Y -12.5);


            Flyout flyout = new Flyout();

            TextBlock tb = new TextBlock();
            tb.Text = "New Comment";

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
            sp.Children.Add(tb);
            sp.Children.Add(it);
            sp.Children.Add(ic);

            flyout.Content = sp;

            rectangle.PointerReleased += async delegate (object s, PointerRoutedEventArgs evt)
            {
                flyout.ShowAt(rectangle);
            };

            canvas.Children.Add(rectangle);

            flyout.ShowAt(rectangle);
        }

        private void ClearStack(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            // clear the stack if a new stroke has been added
            undoStack.Clear();
        }

        private async void saveInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count > 0) {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("Gif with embedded ISF", new List<string> { ".gif" });

                var file = await savePicker.PickSaveFileAsync();

                if (null != file)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
            }
        }

        private async void loadInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".gif");

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
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
