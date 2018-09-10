using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Input;
using System.Diagnostics;
using Squidly.Utils;
using Windows.UI;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using System.Numerics;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Squidly
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // need refactoring, dont think we need a field for this
        public Polyline polyline;

        //fields for stroke selection
        private Polyline lasso;
        private Rect boundingRect;
        private Rectangle boundingBox;
        private bool isBoundRect;
        public bool selectedStrokesExist = false;

        //fields for comments
        public CommentModel comments;
        private Color stickyColor;

        //fields for animation related functionality
        public bool isAnimationMode = false;
        private AnimationModel animations;
        private bool areAllAnimationsRunning = false;
        private Object moveSelectedLock = new Object();
    

        // different mouse looks for normal and when mouse in selected box
        private CoreCursor normalCursor = Window.Current.CoreWindow.PointerCursor;
        private CoreCursor inBoundingBox = new CoreCursor(CoreCursorType.SizeAll, 0);

        //other application settings
        private double canvasWidth { get; set; }
        private double canvasHeight;
        Dictionary<InkStroke, StrokeGroup> strokeGroups = new Dictionary<InkStroke, StrokeGroup>();
        private Save save = null;
        private InkToolbarCustomToolButton animationPen;

        public MainPage()
        {
            this.InitializeComponent();

            canvas.RenderTransform = new TranslateTransform();
            inkCanvas.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                // Uncomment the line below if you want to draw with touch
                // When commented out, long touch to create comment
                // Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            goalsInkCanvas.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                // Uncomment the line below if you want to draw with touch
                // When commented out, long touch to create comment
                // Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            comments = new CommentModel();
            animations = new AnimationModel();

            inkCanvas.InkPresenter.StrokesErased += RemovedStrokes;

            //tool bar set up
            inkToolbar.Loading += InitializeInkToolbar;
            inkToolbar.ActiveToolChanged += InkToolbar_ActiveToolChanged;
            
            //binding animations to front end view
            AnimationRepresentation.ItemsSource = animations.GetAnimations();

            //animation mode set up
            togglePath.Checked += TogglePathChecked;
            togglePath.Unchecked += TogglePathUnchecked;
            Application.Current.Resources["AppBarToggleButtonBackgroundChecked"] = (SolidColorBrush)this.Resources["animationBlockColor"];
            Application.Current.Resources["AppBarToggleButtonBackgroundCheckedPointerOver"] = (SolidColorBrush)this.Resources["animationBlockColor"];
            Application.Current.Resources["AppBarToggleButtonBackgroundCheckedPressed"] = (SolidColorBrush)this.Resources["animationBlockColor"];

            // animation pen set up
            animationPen = new InkToolbarCustomToolButton();
            animationPen.Content = new FontIcon
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uE735",
            };

            //comments set up
            SetUpStickyNotes();
            stickyColor = Colors.Goldenrod;
            toolButtonCommentGlyph.Foreground = new SolidColorBrush(stickyColor);
        }

        /*
         * Set up methods used in constructor above or when elements get loaded
         * */
        private void InitializeInkToolbar(FrameworkElement sender, object args)
        {
            inkToolbar.InitialControls = InkToolbarInitialControls.None;
            InkToolbarBallpointPenButton ballpoint = new InkToolbarBallpointPenButton();
            InkToolbarEraserButton eraser = new InkToolbarEraserButton();
            inkToolbar.Children.Add(eraser);
            inkToolbar.Children.Add(ballpoint);

            inkToolbar.Height = 75;

            toolbarGrid.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            toolbarGrid.ManipulationDelta += new ManipulationDeltaEventHandler(DragToolbar);

        }

        private void DragToolbar(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var rectangle = (Grid)sender;
            var newLeft = Canvas.GetLeft(rectangle) + e.Delta.Translation.X;
            var newTop = Canvas.GetTop(rectangle) + e.Delta.Translation.Y;
            if (newLeft < 0)
            {
                newLeft = 0;
            }
            if (newTop < 0)
            {
                newTop = 0;
            }
            if (newLeft + rectangle.ActualWidth > canvasWidth)
            {
                newLeft = canvasWidth - rectangle.ActualWidth;
            }
            if (newTop + rectangle.ActualHeight > canvasHeight)
            {
                newTop = canvasHeight - rectangle.ActualHeight;
            }
            Canvas.SetLeft(rectangle, newLeft);
            Canvas.SetTop(rectangle, newTop);
        }

        //animation set up methods
        private void TogglePathChecked(object sender, RoutedEventArgs e)
        {
            foreach (var animation in animations.GetAnimations())
            {
                var pline = animation.GetPolyline();
                pline.Opacity = 0.5;
                animation.nameText.Opacity = 0.5;
            }
        }

        private void TogglePathUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var animation in animations.GetAnimations())
            {
                var pline = animation.GetPolyline();
                pline.Opacity = 0;
                animation.nameText.Opacity = 0;
            }
        }

        //timeline in this case isn't in time units. It's based on the horizontal positions in the canvas. Length units of canvas have been directly mapped to time units.
        //1 unit of length of the Canvas is around 16.5 ms.
        //this method will also load the animation toolbar
        private void TimeLineCanvasLoaded(object sender, RoutedEventArgs e)
        {
            var length = timelineCanvas.ActualWidth;
            for (int i = 0; i < length; i=i+50)
            {
                TextBlock time = new TextBlock()
                {
                    Text = i.ToString(),
                    FontWeight = Windows.UI.Text.FontWeights.Light,
                    FontSize=10
                };
                Canvas.SetLeft(time, i);
                timelineCanvas.Children.Add(time);
            }
        }

        private void SetUpStickyNotes()
        {
            Color[] colors = { Colors.Goldenrod, Colors.Plum, Colors.LightSkyBlue, Colors.PaleGreen };
            foreach (Color c in colors)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 36,
                    Height = 36,
                    Margin = new Thickness(8),
                    Fill = new SolidColorBrush(c),
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Colors.Black),
                };
                ellipse.Tapped += ChangeStickyColor;
                ellipse.PointerEntered += StickyPointerHover;
                ellipse.PointerExited += StickyPointerExit;
                ellipse.PointerPressed += StickyPointerPress;
                ellipse.PointerReleased += StickyPointerExit;
                
                StickyNoteStack.Children.Add(ellipse);
            }
           
        }

        private void StickyPointerPress(object sender, PointerRoutedEventArgs e)
        {
            var ellipse = sender as Ellipse;
            ellipse.Stroke = new SolidColorBrush(Colors.White);

        }

        private void StickyPointerExit(object sender, PointerRoutedEventArgs e)
        {
            var ellipse = sender as Ellipse;
            ellipse.Stroke = new SolidColorBrush(Colors.Black);
        }

        private void StickyPointerHover(object sender, PointerRoutedEventArgs e)
        {
            var ellipse = sender as Ellipse;
            ellipse.Stroke = ellipse.Fill;
        }

        private void ChangeStickyColor(object sender, TappedRoutedEventArgs e)
        {
            var ellipse = sender as Ellipse;
            stickyColor = (ellipse.Fill as SolidColorBrush).Color;
            toolButtonCommentGlyph.Foreground = new SolidColorBrush(stickyColor);
            StickyFlyout.Hide();
        }


        // Anonymous
        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            canvasWidth = canvas.ActualWidth;
            canvasHeight = canvas.ActualHeight;
        }


        private void BackToMenu(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Home));
        }

        /*
         * Saving and loading functionality
         * */

        public async void SaveAll(object sender, RoutedEventArgs e)
        {
            if (save == null)
            {
                save = new Save();
                // display some sort of selection screen
                await save.CreateFolder();
            
            }
            await save.SaveAll(inkCanvas, goalsInkCanvas, comments, animations);
        }

        public void SaveHelper()
        {
            if (comments != null)
            {
                polyCanvas.Children.Clear();
                canvas.Children.Clear(); // probably better way than this...
                Debug.WriteLine(comments.GetComments().Count);
                foreach (Comment c in comments.GetComments())
                {
                    DrawRectangle(c);
                }

            }
            if (animations != null)
            {
                foreach (Animation a in animations.GetAnimations())
                {

                    for (int i = 0; i < a.inkStrokesId.Count; i++)
                    {
                        a.inkStrokesId[i] = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[a.inkStrokesIndex[i]].Id;
                    }

                    // recreate polyline!
                    polyline = new Polyline()
                    {
                        Stroke = new SolidColorBrush(Windows.UI.Colors.ForestGreen),
                        StrokeThickness = 1.5,
                        StrokeDashArray = new DoubleCollection() { 5, 2 },
                    };
                    polyline.Points = a.linePoints;

                    polyline.Opacity = togglePath.IsChecked == true ? 0.5 : 0;
                    a.nameText.Opacity = togglePath.IsChecked == true ? 0.5 : 0;
                    a.SetPolyline(polyline);
                    polyCanvas.Children.Add(polyline);
                    addPolylineText(a);
                }
            }
        }

        public async void LoadAll(object sender, RoutedEventArgs e)
        {
            if (save == null)
            {
                save = new Save();
            }

            await save.LoadAll(inkCanvas, goalsInkCanvas, comments, animations);
            SaveHelper();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is bool && (bool)e.Parameter == true)
            {
                if (save == null)
                {
                    save = new Save();
                }

                await save.LoadAll(inkCanvas, goalsInkCanvas, comments, animations);
            } else if (e.Parameter is Save)
            {
                save = e.Parameter as Save;
                await save.LoadNew(inkCanvas, goalsInkCanvas, comments, animations);
            }

            SaveHelper();
            base.OnNavigatedTo(e);
        }


        /**
         * below are functions for the different modes in the tool bar
         * */
        // changing mode in tool bar
        private void InkToolbar_ActiveToolChanged(InkToolbar sender, object args)
        {
            ClearAllHandlers();

            if (inkToolbar.Children.Contains(animationPen))
            {
                inkToolbar.Children.Remove(animationPen);
            }

            if (inkToolbar.ActiveTool == toolButtonLasso)
            {
                inkCanvas.RightTapped += ClickSelect;
                //for passing modified input to the app for custom processing
                inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

                //Listeners for unprocessed pointer events from the modified input
                inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
                inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
                inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;

                //Listeners for new ink or erase strokes so that selection could be cleared when inking or erasing is detected
                inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
                inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
            }
            else if (inkToolbar.ActiveTool == toolButtonComment)
            {
                inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
                inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += MakeComment;
            }
        }

        private void ClearAllHandlers()
        {
            selectionCanvas.Children.Clear(); //removes bounding box from GUI
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
            inkCanvas.RightTapped -= ClickSelect;
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= MakeComment;
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted -= StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased -= InkPresenter_StrokesErased;
        }

        /*
        * Comment related methods for comment mode in tool bar
        * */
        private void MakeComment(InkUnprocessedInput sender, PointerEventArgs args)
        {
            PointerPoint point = args.CurrentPoint;
            var newComment = comments.CreateComment(point.Position.X, point.Position.Y, stickyColor);
            var rect = DrawRectangle(newComment);
            rect.ContextFlyout.ShowAt(rect);
        }

        // drawing a comment rectangle - comment flyout when pressed
        public Rectangle DrawRectangle(Comment comment)
        {
            Rectangle rectangle = new Rectangle
            {
                Fill = new SolidColorBrush(comment.fill),
                Width = comment.width,
                Height = comment.height,
                Opacity = comment.opacity,
                RenderTransform = new RotateTransform { Angle = comment.angle }
            };

            rectangle.DataContext = comment;
            Canvas.SetLeft(rectangle, comment.left);
            Canvas.SetTop(rectangle, comment.top);

            // Add flyout
            var flyout = new Flyout();
            Style flyoutStyle = new Style();
            flyoutStyle.TargetType = typeof(FlyoutPresenter);
            flyoutStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(comment.fill)));
            flyout.FlyoutPresenterStyle = flyoutStyle;

            // Add delete button
            Button deleteButton = new Button();
            deleteButton.Content = new SymbolIcon(Symbol.Delete);
            deleteButton.Click += delegate (object e, RoutedEventArgs evt)
            {
                canvas.Children.Remove(rectangle);
                comments.Remove(comment);
            };

            // Add canvas
            InkCanvas flyoutInkCanvas = new InkCanvas
            {
                Width = 250,
                Height = 250
            };
            flyoutInkCanvas.InkPresenter.InputDeviceTypes =
              Windows.UI.Core.CoreInputDeviceTypes.Mouse |
              Windows.UI.Core.CoreInputDeviceTypes.Touch |
              Windows.UI.Core.CoreInputDeviceTypes.Pen;

            // Set up stroke container for serialization
            if (comment.ic != null)
            {
                flyoutInkCanvas.InkPresenter.StrokeContainer = comment.ic;
            }
            comment.ic = flyoutInkCanvas.InkPresenter.StrokeContainer;

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
            //flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;
            rectangle.ContextFlyout = flyout;

            // settings for dragging comments 
            rectangle.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            rectangle.ManipulationDelta += new ManipulationDeltaEventHandler(DragComment);
            canvas.Children.Add(rectangle);

            return rectangle;
        }

        private void DragComment(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var rectangle = (Rectangle)sender;
            var newLeft = Canvas.GetLeft(rectangle) + e.Delta.Translation.X;
            var newTop = Canvas.GetTop(rectangle) + e.Delta.Translation.Y;
            if (newLeft < 0)
            {
                newLeft = 0;
            }
            if (newTop < 0)
            {
                newTop = 0;
            }
            if (newLeft + rectangle.ActualWidth > canvasWidth)
            {
                newLeft = canvasWidth - rectangle.ActualWidth;
            }
            if (newTop + rectangle.ActualHeight > canvasHeight)
            {
                newTop = canvasHeight - rectangle.ActualHeight;
            }
            Canvas.SetLeft(rectangle, newLeft);
            Canvas.SetTop(rectangle, newTop);

            Comment c = (Comment)rectangle.DataContext;
            c.left = newLeft;
            c.top = newTop;

            
        }


        /*
         * Stroke selection functionality (lasso) for selection mode in tool bar
         * */
         // Handlers for lasso
        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            //polyline draws a series of connected straight lines - going to pass it points where the pen is
            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);
            selectionCanvas.Children.Add(lasso);
            isBoundRect = true;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (isBoundRect)
            {
                lasso.Points.Add(args.CurrentPoint.RawPosition);
            }
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            //add final point to the Polyline object
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(lasso.Points);

            List<InkStroke> selectedStrokes = new List<InkStroke>();
            foreach(InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes()) {
                if (stroke.Selected)
                {
                    selectedStrokes.Add(stroke);
                }
            }
            boundingRect = FindBoundingRect(selectedStrokes);
            isBoundRect = false;
            DrawBoundingRect();
        }

        //handle new ink or erase strokes to clean up Selection UI 
        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        private void RemovedStrokes(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            List<Animation> animationsToRemove = new List<Animation>();
            // look for strokes that have animations
            foreach (var inkstroke in args.Strokes)
            {
                var id = inkstroke.Id;
                foreach (Animation a in animations.GetAnimations())
                {
                    foreach (uint stroke in a.inkStrokesId)
                    {
                        if (stroke == id)
                        {
                            // delete the entire animation if constituent stroke killed
                            animationsToRemove.Add(a);
                            Debug.WriteLine("removing...");
                            break;

                        }
                    }
                }
            }

            foreach (Animation a in animationsToRemove)
            {
                animations.GetAnimations().Remove(a);
                polyCanvas.Children.Remove(a.GetPolyline());
                polyCanvas.Children.Remove(a.nameText);
            }
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            ClearSelection();
            
        }

        private void ClickSelect(object sender, RightTappedRoutedEventArgs args)
        {
            Point clickedPoint = args.GetPosition(inkCanvas);
            //need to adjust it so that it works for different thickness strokes
            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithLine(new Point(clickedPoint.X - 1, clickedPoint.Y - 1), new Point(clickedPoint.X, clickedPoint.Y + 3));

            List<InkStroke> selectedStrokes = new List<InkStroke>();
            foreach (InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    selectedStrokes.Add(stroke);
                }
            }
            boundingRect = FindBoundingRect(selectedStrokes);
            DrawBoundingRect();
        }

        //clear existing content from the selection layer and draw a single bounding rectangle around the ink strokes encompassed by the lasso area
        private void DrawBoundingRect()
        {
            selectionCanvas.Children.Clear();

            //draw bounding box only if there are ink strokes within the lasso
            if (!((boundingRect.Width == 0) || (boundingRect.Height == 0) || boundingRect.IsEmpty))
            {
                selectedStrokesExist = true;
                SolidColorBrush transparent = new SolidColorBrush(Windows.UI.Colors.Coral);
                transparent.Opacity = 0;
                var rectangle = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                    Width = boundingRect.Width,
                    Height = boundingRect.Height,
                    Fill = transparent
                };
                AddContextMenu(rectangle);

                Canvas.SetLeft(rectangle, boundingRect.X);
                Canvas.SetTop(rectangle, boundingRect.Y);
                boundingBox = rectangle;
                rectangle.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                rectangle.ManipulationDelta += new ManipulationDeltaEventHandler(DragStroke); //TODO on release edit hte matrix3x2 so that new position is origin?
                rectangle.PointerEntered += new PointerEventHandler(CursorInBoundingBox);
                rectangle.PointerExited += new PointerEventHandler(CursorLeaveBoundingBox);

                selectionCanvas.Children.Add(rectangle);
            }
            else
            {
                selectedStrokesExist = false;
            }
        }

        // change cursor when it is in a selection box
        private void CursorInBoundingBox(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = inBoundingBox;
        }

        private void CursorLeaveBoundingBox(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = normalCursor;
        }

        // check for stroke groups and update selected strokes if original stroke(s) in stroke group
        private Rect FindBoundingRect(List<InkStroke> strokes)
        {
            foreach (InkStroke stroke in strokes)
            {
                stroke.Selected = true;
            }
            StrokeGroup strokeGroup;
            boundingRect = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(0,0));
            Rect updatedBoundingBox = boundingRect;
            var updatedLeftX = updatedBoundingBox.X;
            var updatedRightX = updatedBoundingBox.X + updatedBoundingBox.Width;
            var updatedTopY = updatedBoundingBox.Y;
            var updatedBottomY = updatedBoundingBox.Y + updatedBoundingBox.Height;

            foreach (var stroke in strokes)
            {
                if (strokeGroups.TryGetValue(stroke, out strokeGroup))
                {
                    strokeGroup.SelectStrokesInGroup();
                    Rect groupBoundingBox = strokeGroup.FindBoundingBox();

                    if (groupBoundingBox.X < updatedBoundingBox.X)
                    {
                        updatedLeftX = groupBoundingBox.X;
                    }

                    if (groupBoundingBox.Y < updatedBoundingBox.Y)
                    {
                        updatedTopY = groupBoundingBox.Y;
                    }

                    if (groupBoundingBox.X + groupBoundingBox.Width > updatedBoundingBox.X + updatedBoundingBox.Width)
                    {
                        updatedRightX = groupBoundingBox.X + groupBoundingBox.Width;
                    }

                    if (groupBoundingBox.Y + groupBoundingBox.Height > updatedBoundingBox.Y + updatedBoundingBox.Height)
                    {
                        updatedBottomY = groupBoundingBox.Y + groupBoundingBox.Height;
                    }
                }

                updatedBoundingBox = new Rect(updatedLeftX, updatedTopY, updatedRightX - updatedLeftX, updatedBottomY - updatedTopY);
            }
            foreach (InkStroke stroke in strokes)
            {
                stroke.Selected = false;
            }
            return updatedBoundingBox;
        }

        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearDrawnBoundingRect();
            selectedStrokesExist = false;
        }

        private void ClearDrawnBoundingRect()
        {
            if (selectionCanvas.Children.Any())
            {
                selectionCanvas.Children.Clear();
                boundingRect = Rect.Empty;
                boundingBox = null;
            }
        }

        private void DragStroke(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var rectangle = (Rectangle)sender;
            var newLeft = Canvas.GetLeft(rectangle) + e.Delta.Translation.X;
            var newTop = Canvas.GetTop(rectangle) + e.Delta.Translation.Y;
            var xTranslation = e.Delta.Translation.X;
            var yTranslation = e.Delta.Translation.Y;
            if (newLeft < 0)
            {
                newLeft = 0;
                xTranslation = 0;
            }
            if (newTop < 0)
            {
                newTop = 0;
                yTranslation = 0;
            }
            if (newLeft + rectangle.ActualWidth > canvasWidth)
            {
                newLeft = canvasWidth - rectangle.ActualWidth;
                xTranslation = 0;
            }
            if (newTop + rectangle.ActualHeight > canvasHeight)
            {
                newTop = canvasHeight - rectangle.ActualHeight;
                yTranslation = 0;
            }
            inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(xTranslation, yTranslation));
            Canvas.SetLeft(rectangle, newLeft);
            Canvas.SetTop(rectangle, newTop);
            boundingRect = new Rect(newLeft, newTop, rectangle.ActualWidth, rectangle.ActualHeight);
        }


        //add context menu to selected strokes
        private void AddContextMenu(Rectangle boundingBox)
        {
            MenuFlyoutItem item1 = new MenuFlyoutItem { Text = "Group strokes" };
            MenuFlyoutItem item2 = new MenuFlyoutItem { Text = "Draw path" };
            MenuFlyoutItem item3 = new MenuFlyoutItem { Text = "Delete" };
            MenuFlyoutItem item4 = new MenuFlyoutItem { Text = "Duplicate" };
            item1.Click += new RoutedEventHandler(CombineStrokes);
            item2.Click += new RoutedEventHandler(DrawPathSelectedStrokes);
            item3.Click += new RoutedEventHandler(DeleteSelectedStrokes);
            item4.Click += new RoutedEventHandler(DuplicateSelectedStrokes);


            MenuFlyout flyout = new MenuFlyout();
            flyout.Items.Add(item1);
            flyout.Items.Add(item2);
            flyout.Items.Add(item3);
            flyout.Items.Add(item4);

            boundingBox.ContextFlyout = flyout;
        }

        //selected strokes context menu functionality
        private void DeleteSelectedStrokes(object sender, RoutedEventArgs e)
        {
            List<Animation> animationsToRemove = new List<Animation>();

            foreach (var inkstroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                if (inkstroke.Selected)
                {
                    var id = inkstroke.Id;
                    foreach (Animation a in animations.GetAnimations())
                    {
                        foreach (uint stroke in a.inkStrokesId)
                        {
                            if (stroke == id)
                            {
                                animationsToRemove.Add(a);
                                break;
                            }
                        }
                    }
                }
                
            }
            foreach (Animation a in animationsToRemove)
            {
                animations.GetAnimations().Remove(a);
                polyCanvas.Children.Remove(a.GetPolyline());
                polyCanvas.Children.Remove(a.nameText);
            }
            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            ClearDrawnBoundingRect();
            ClearSelection();
        }

        //selected strokes context menu functionality
        private void DuplicateSelectedStrokes(object sender, RoutedEventArgs e)
        {
            //var numStrokesBefore = strokes.Count();

            inkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
            boundingRect = inkCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(new Point(Canvas.GetLeft(boundingBox), Canvas.GetTop(boundingBox)));
            boundingRect.X += 20;
            boundingRect.Y -= 20;

            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(20, -20));

            DrawBoundingRect();
        }

        //selected strokes context menu functionality
        private void DrawPathSelectedStrokes(Object sender, RoutedEventArgs e)
        {
            runAllAnimationsButton.IsEnabled = false;
            //TODO We still want the canvas we just want to hide it.
            selectionCanvas.Visibility = Visibility.Collapsed;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            var currentTool = inkToolbar.ActiveTool;


            inkToolbar.ActiveTool = animationPen;
            inkToolbar.Children.Add(animationPen);

            void pressed(InkUnprocessedInput i, PointerEventArgs p)
            {
                polyline = new Polyline()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.ForestGreen),
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                };
                polyline.Points.Add(p.CurrentPoint.Position);
                polyCanvas.Children.Add(polyline);



            }

            void moved(InkUnprocessedInput i, PointerEventArgs p)
            {

                polyline.Points.Add(p.CurrentPoint.Position);

            }

            async void released(InkUnprocessedInput i, PointerEventArgs p)
            {
                polyline.Points.Add(p.CurrentPoint.Position);
                polyline.Opacity = 0.5;
                inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.AllowProcessing;
                inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= pressed;
                inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= moved;
                inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= released;
                ClearAllHandlers();
                Animation anime = new Animation();
                foreach (var stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                {
                    if (stroke.Selected)
                    {
                        anime.inkStrokes.Add(stroke);
                        anime.inkStrokesId.Add(stroke.Id);
                        anime.inkStrokesIndex.Add(inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList().IndexOf(stroke));
                    }
                }
                anime.SetPolyline(polyline);
                addPolylineText(anime);
                inkToolbar.ActiveTool = currentTool;
                inkToolbar.Children.Remove(animationPen);
                var container = inkCanvas.InkPresenter.StrokeContainer;

                animations.Add(anime);


                await RunAnimation(anime, true);
                selectionCanvas.Visibility = Visibility.Visible; // this is actually a workaround, we just want to hide the current selection box

                ClearSelection();
                runAllAnimationsButton.IsEnabled = true;
            }
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += pressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += moved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += released;
        }

        private void addPolylineText(Animation animation)
        {
            TextBlock tb = new TextBlock();
            tb.DataContext = animation;
            Binding binding = new Binding { Path = new PropertyPath("name") };
            tb.SetBinding(TextBlock.TextProperty, binding);
            Canvas.SetLeft(tb, animation.polyline.Points[0].X);
            Canvas.SetTop(tb, animation.polyline.Points[0].Y);


            //Canvas.SetLeft(animation.nameText, animation.polyline.Points[0].X);
            //Canvas.SetTop(animation.nameText, animation.polyline.Points[0].Y);
            //polyCanvas.Children.Add(animation.nameText);
            polyCanvas.Children.Add(tb);
        }

        private void CombineStrokes(object sender, RoutedEventArgs e)
        {
            bool selectedExists = false;
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            List<InkStroke> selectedStrokes = new List<InkStroke>();
            StrokeGroup strokeGroup = new StrokeGroup();
            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    selectedExists = true;
                    selectedStrokes.Add(stroke);

                    if (strokeGroups.ContainsKey(stroke))
                    {
                        strokeGroups.TryGetValue(stroke, out strokeGroup);
                        strokeGroup.AddStroke(stroke);
                    } else
                    {
                        strokeGroup.AddStroke(stroke);
                        strokeGroups.Add(stroke, strokeGroup);
                    }
                }
            }

            if (selectedExists)
            {
                ClearSelection();
            }
        }


        /**
         * Animation related methods
         **/
        
        /*
         * methods to do with manipulating animations
         **/

        private async Task RunAnimation(Animation animation, bool revert)
        {
            //TODO: Check if the inkstrokes of the animation still exists...
            List<InkStroke> strokesToAnimate = new List<InkStroke>();
            ClearSelection();

            foreach (var s in animation.inkStrokesId)
            {
                // check if stroke still exists
                var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s);
                if (stroke != null && inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Contains(stroke))
                {
                    strokesToAnimate.Add(stroke);
                }
            }

            if (strokesToAnimate.Count == 0)
            {
                // we can delete this current animation entry
                animations.GetAnimations().Remove(animation);
                polyCanvas.Children.Remove(animation.GetPolyline());
                polyCanvas.Children.Remove(animation.nameText);
                return;
            }

            var delta = animation.startPoint;

            var pline = animation.GetPolyline();
            pline.Opacity = 1;
            animation.isActive = true;
            animation.nameText.Opacity = 1;



            Rect currentPosition; 
            lock (moveSelectedLock)
            {
                currentPosition = FindBoundingRect(strokesToAnimate);
            }

            foreach (InkStroke stroke in strokesToAnimate)
            {
                stroke.PointTransform = Matrix3x2.CreateTranslation((float)(animation.startPoint.X - (currentPosition.X + currentPosition.Width / 2) + stroke.PointTransform.Translation.X), (float)(animation.startPoint.Y - (currentPosition.Y + currentPosition.Height / 2) + stroke.PointTransform.Translation.Y));
            }

            var i = -1;
            //Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (Point pt in animation.GetPolyline().Points)
            {
                foreach (InkStroke stroke in strokesToAnimate)
                {
                    stroke.PointTransform = Matrix3x2.CreateTranslation((float)(pt.X - delta.X + stroke.PointTransform.Translation.X), (float)(pt.Y - delta.Y + stroke.PointTransform.Translation.Y));
                }
                delta = pt;
                await Task.Delay(TimeSpan.FromSeconds(0.001));
                i++;
            }
            //stopwatch.Stop();
            //Debug.WriteLine("ms " + stopwatch.ElapsedMilliseconds);

            if (revert)
            {
                lock (moveSelectedLock)
                {
                    currentPosition = FindBoundingRect(strokesToAnimate);
                }

                foreach (InkStroke stroke in strokesToAnimate)
                {
                    stroke.PointTransform = Matrix3x2.CreateTranslation((float)(animation.startPoint.X - (currentPosition.X + currentPosition.Width / 2) + stroke.PointTransform.Translation.X), (float)(animation.startPoint.Y - (currentPosition.Y + currentPosition.Height / 2) + stroke.PointTransform.Translation.Y));
                }
            }

            pline.Opacity = togglePath.IsChecked == true ? 0.5 : 0;
            animation.nameText.Opacity = togglePath.IsChecked == true ? 0.5 : 0;
            animation.isActive = false;


            foreach (var stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                stroke.Selected = false;
            }

        }

        private async void RunAllAnimations(object sender, RoutedEventArgs e)
        {
            runAllAnimationsButton.IsEnabled = false;

            var tasks = new List<Task>();
            //15.638 on my computer
            var msPerPoint = 16.560;
            SortedSet<Animation> orderedAnimationList = new SortedSet<Animation>(new AnimationComparer());
            double previousStart = 0;
            foreach(Animation a in AnimationRepresentation.Items)
            {
                orderedAnimationList.Add(a);
                a.IsEnabled = false;
            }

            foreach (Animation a in orderedAnimationList)
            {
                await Task.Delay(TimeSpan.FromMilliseconds((a.position - previousStart) * msPerPoint));
                tasks.Add(RunAnimation(a, resetButton.IsChecked == true));
                previousStart = a.position;
            }

            await Task.WhenAll(tasks);
            foreach(Animation a in AnimationRepresentation.Items)
            {
                a.IsEnabled = true;
            }
            runAllAnimationsButton.IsEnabled = true;

        }

        //replay selected animation
        private async void Replay(object sender, RoutedEventArgs e)
        {
            FrameworkElement b = sender as FrameworkElement;
            Animation a = b.DataContext as Animation;

            runAllAnimationsButton.IsEnabled = false;
            a.IsEnabled = false;

            int index = a.id;
            var replayAnimation = animations.GetAnimationAt(index); // won't work once we start deleting
            await RunAnimation(replayAnimation, resetButton.IsChecked == true);

            runAllAnimationsButton.IsEnabled = true;
            a.IsEnabled = true;
        }

 

        private async void Query(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement b = sender as FrameworkElement;
            Animation a = b.DataContext as Animation;
            int index = a.id;
            var animation = animations.GetAnimationAt(index);
            animation.polyline.Stroke = new SolidColorBrush(Colors.Crimson);
            animation.polyline.Opacity = 1.0;
            
        }

        private async void QueryStop(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement b = sender as FrameworkElement;
            Animation a = b.DataContext as Animation;
            int index = a.id;
            var animation = animations.GetAnimationAt(index);

            if (animation != null)
            {
                animation.polyline.Stroke = new SolidColorBrush(Colors.ForestGreen);

                if (animation.isActive)
                {
                    animation.polyline.Opacity = 1;
                }
                else if (togglePath.IsChecked == true)
                {
                    animation.polyline.Opacity = 0.5;
                }
                else
                {
                    animation.polyline.Opacity = 0;
                }
            }
            
        }

        private void DeleteAnimation(object sender, RoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            Animation a = senderElement.DataContext as Animation;

            int index = a.id;
            var anime = animations.GetAnimationAt(index);

            if (anime != null)
            {
                polyCanvas.Children.Remove(anime.GetPolyline());

                animations.RemoveAnimation(index);
            }
            
        }

        //currently not doing anything
        private void SettingsAnimation(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            Animation a = b.DataContext as Animation;
            int index = a.id;

            Flyout f = new Flyout();
            b.Flyout = f;
        }

        /*
         * methods for animation mode
         * */

        private void DragAnimationChunk(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var textblock = (Border)sender;
            var newLeft = Canvas.GetLeft(textblock) + e.Delta.Translation.X;
            var newTop = Canvas.GetTop(textblock) + e.Delta.Translation.Y;

            if (newLeft < 0)
            {
                newLeft = 0;
            }
            if (newLeft + textblock.ActualWidth > canvasWidth - 40)
            {
                newLeft = canvasWidth - textblock.ActualWidth - 40;
            }

            Canvas.SetLeft(textblock, newLeft);
            Canvas.SetTop(textblock, newTop);

            Animation animation = (Animation) textblock.DataContext;
            animation.position = newLeft;
        }

        private async void OpenRenameAnimationDialog(object sender, RoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            Animation a = senderElement.DataContext as Animation;
            int index = a.id;

            renameUserInput.Text = String.Empty;
            ContentDialogResult userAction = await renameDialog.ShowAsync();

            if (userAction == ContentDialogResult.Primary)
            {
                Animation nameChange = animations.GetAnimationAt(index);
                RenameAnimation(renameUserInput.Text, nameChange);
            }
        }

        private void RenameAnimation(String newName, Animation animation)
        {
            animation.Name = newName;
        }

        private void UserInputTextChanged(object sender, RoutedEventArgs e)
        {
            TextBox renameTextbox = (TextBox)sender;
            String userInput = renameTextbox.Text.Trim();

            if (userInput.Length > 0 && !animations.DoesNameExist(userInput))
            {
                renameTextbox.BorderBrush = new SolidColorBrush(Colors.Green);
                renameDialog.IsPrimaryButtonEnabled = true;
            } else if (animations.DoesNameExist(userInput))
            {
                renameTextbox.BorderBrush = new SolidColorBrush(Colors.Red);
                renameDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                renameTextbox.BorderBrush = new SolidColorBrush(Colors.Red);
                renameDialog.IsPrimaryButtonEnabled = false;
            }
        }

    }
}
  