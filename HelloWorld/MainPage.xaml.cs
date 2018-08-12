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
using Protocol2.Utils;
using Windows.UI;
using Windows.UI.Core;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Protocol2
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
    

        // different mouse looks for normal and when mouse in selected box
        private CoreCursor normalCursor = Window.Current.CoreWindow.PointerCursor;
        private CoreCursor inBoundingBox = new CoreCursor(CoreCursorType.SizeAll, 0);

        //other application settings
        private double canvasWidth;
        private double canvasHeight;
        Dictionary<InkStroke, StrokeGroup> strokeGroups = new Dictionary<InkStroke, StrokeGroup>();
        private Save save = null;

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

            comments = new CommentModel();
            animations = new AnimationModel();

            //tool bar set up
            inkToolbar.Loading += InitializeInkToolbar;
            inkToolbar.ActiveToolChanged += InkToolbar_ActiveToolChanged;

            //binding animations to front end view
            Animationlist.ItemsSource = animations.GetAnimations();
            AnimationRepresentation.ItemsSource = animations.GetAnimations();

            //animation mode set up
            AnimationMode.Checked += AnimationToggleChecked;
            AnimationMode.Unchecked += AnimationToggleUnchecked;

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
        private void AnimationToggleChecked(object sender, RoutedEventArgs e)
        {
            foreach (var animation in animations.GetAnimations())
            {
                var pline = animation.GetPolyline();
                pline.Opacity = 0.3;
            }
        }

        private void AnimationToggleUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var animation in animations.GetAnimations())
            {
                var pline = animation.GetPolyline();
                pline.Opacity = 0;
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

        private void ToggleActionBarPressed(Object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
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
            await save.SaveAll(inkCanvas, comments, animations);
        }

        public async void LoadAll(object sender, RoutedEventArgs e)
        {
            if (save == null)
            {
                save = new Save();
            }

            await save.LoadAll(inkCanvas, comments, animations);
            if (comments != null)
            {
                canvas.Children.Clear(); // probably better way than this...
                Debug.WriteLine(comments.GetComments().Count);
                foreach(Comment c in comments.GetComments())
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
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection() { 5, 2 },
                    };
                    polyline.Points = a.linePoints;
                    polyline.Opacity = AnimationMode.IsChecked == true ? 0.3 : 0;
                    a.SetPolyline(polyline);
                    canvas.Children.Add(polyline);
                }
            }
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is bool && (bool)e.Parameter == true)
            {
                if (save == null)
                {
                    save = new Save();
                }

                await save.LoadAll(inkCanvas, comments, animations);
                if (comments != null)
                {
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
                            StrokeThickness = 3,
                            StrokeDashArray = new DoubleCollection() { 5, 2 },
                        };
                        polyline.Points = a.linePoints;
                        polyline.Opacity = AnimationMode.IsChecked == true ? 0.3 : 0;
                        a.SetPolyline(polyline);
                        canvas.Children.Add(polyline);
                    }
                }
            }
            else if (e.Parameter is Save)
            {
                save = e.Parameter as Save;
                await save.LoadNew(inkCanvas, comments, animations);
                if (comments != null)
                {
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
                            StrokeThickness = 3,
                            StrokeDashArray = new DoubleCollection() { 5, 2 },
                        };
                        polyline.Points = a.linePoints;
                        polyline.Opacity = AnimationMode.IsChecked == true ? 0.3 : 0;
                        a.SetPolyline(polyline);
                        canvas.Children.Add(polyline);
                    }
                }
            }
            base.OnNavigatedTo(e);
        }



        /**
         * below are functions for the different modes in the tool bar
         * */
        // changing mode in tool bar
        private void InkToolbar_ActiveToolChanged(InkToolbar sender, object args)
        {
            ClearAllHandlers();
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
            
            UpdateSelected(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());

            isBoundRect = false;
            DrawBoundingRect();
        }

        //handle new ink or erase strokes to clean up Selection UI 
        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
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
            UpdateSelected(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
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
        private void UpdateSelected(IReadOnlyList<InkStroke> strokes)
        {
            StrokeGroup strokeGroup;
            Rect updatedBoundingBox = boundingRect;
            var updatedLeftX = updatedBoundingBox.X;
            var updatedRightX = updatedBoundingBox.X + updatedBoundingBox.Width;
            var updatedTopY = updatedBoundingBox.Y;
            var updatedBottomY = updatedBoundingBox.Y + updatedBoundingBox.Height;

            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    if (strokeGroups.TryGetValue(stroke, out strokeGroup))
                    {
                        strokeGroup.selectStrokesInGroup();
                        Rect groupBoundingBox = strokeGroup.findBoundingBox();

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
                }

                updatedBoundingBox = new Rect(updatedLeftX, updatedTopY, updatedRightX - updatedLeftX, updatedBottomY - updatedTopY);
            }
            boundingRect = updatedBoundingBox;
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
            //TODO We still want the canvas we just want to hide it.
            selectionCanvas.Visibility = Visibility.Collapsed;
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            var currentTool = inkToolbar.ActiveTool;
            var animationPen = new InkToolbarCustomToolButton();
            inkToolbar.ActiveTool = animationPen;
            inkToolbar.Children.Add(animationPen);

            void pressed(InkUnprocessedInput i, PointerEventArgs p)

            {
                polyline = new Polyline()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.ForestGreen),
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                };

                polyline.Points.Add(p.CurrentPoint.Position);
                canvas.Children.Add(polyline);
            }

            void moved(InkUnprocessedInput i, PointerEventArgs p)
            {

                polyline.Points.Add(p.CurrentPoint.Position);

            }

            async void released(InkUnprocessedInput i, PointerEventArgs p)
            {
                polyline.Points.Add(p.CurrentPoint.Position);
                polyline.Opacity = 0.3;
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

                inkToolbar.ActiveTool = currentTool;
                inkToolbar.Children.Remove(animationPen);
                var container = inkCanvas.InkPresenter.StrokeContainer;

                animations.Add(anime);

                //canvas.Children.Remove(polyline); //maybe only show when flyout or something...

                await Animate(anime, true);
                selectionCanvas.Visibility = Visibility.Visible; // this is actually a workaround, we just want to hide the current selection box

                ClearSelection();
            }
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += pressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += moved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += released;
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

        // play animation for single animation
        private async Task Animate(Animation animation, bool revert)
        {
            //TODO: Check if the inkstrokes of the animation still exists...
            List<InkStroke> strokesToAnimate = new List<InkStroke>();
            foreach (var stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                stroke.Selected = false;
            }
            //foreach (var stroke in animation.GetInkStrokes())
            //{
            //    stroke.Selected = true;
            //}
            //foreach (var strokeid in animation.inkStrokesIndex)
            //{
            //    inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(strokeid).Selected = true;
            //}
            //foreach (var s in animation.inkStrokesIndex)
            //{
            //    if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Contains(inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(s)))
            //    {
            //        strokesToAnimate.Add(inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(s));
            //    }
            //}
            foreach (var s in animation.inkStrokesId)
            {
                // check if stroke still exists
                var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s);
                if (stroke != null && inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Contains(stroke))
                {
                    inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s).Selected = true;

                    strokesToAnimate.Add(inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s));
                }


            }

            if (strokesToAnimate.Count == 0)
            {
                // we can delete this current animation entry
                animations.GetAnimations().Remove(animation);
                canvas.Children.Remove(animation.GetPolyline());
                return;
            }

            var delta = animation.startPoint;

                var pline = animation.GetPolyline();
                pline.Opacity = 1;

            //canvas.Children.Add(animation.GetPolyline());
            Rect currentPosition = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(0,0));
            

            inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(animation.startPoint.X  - (currentPosition.X + currentPosition.Width/2), animation.startPoint.Y - (currentPosition.Y + currentPosition.Height / 2)));
    
            // want something here so we reset the location of ink to where it should start from
            // MoveStroke doesn't move it to a position relative to the canvas but rather relative to its current location!
            
            var i = -1;
            foreach (Point pt in animation.GetPolyline().Points)
            {
                //container.MoveSelected(new Point(pt.X - prevX, pt.Y - prevY));
                var r = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(pt.X - delta.X, pt.Y - delta.Y));
                delta = pt;
                await Task.Delay(TimeSpan.FromSeconds(0.001));
                i++;

            }

            if (revert)
            {
                currentPosition = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));


                inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(animation.startPoint.X - (currentPosition.X + currentPosition.Width / 2), animation.startPoint.Y - (currentPosition.Y + currentPosition.Height / 2)));

            }

            if (AnimationMode.IsChecked == true)
            {
                pline.Opacity = 0.3;

            } else
            {
                pline.Opacity = 0;
            }
        }

        //run animations for all existing animations
        //currently not using Animate method, but probably can
        private async void RunAllAnimations(object sender, RoutedEventArgs e)
        {

            List<Animation> allAnimations = animations.GetAnimations().ToList();
            foreach (var animation in allAnimations) 
            {
                List<InkStroke> strokesToAnimate = new List<InkStroke>();
                foreach (var stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                {
                    stroke.Selected = false;
                }
                //foreach (var strokeid in animation.inkStrokesIndex)
                //{
                //    inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(strokeid).Selected = true;
                //}
                //foreach (var s in animation.inkStrokesIndex)
                //{
                //    if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Contains(inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(s)))
                //    {
                //        strokesToAnimate.Add(inkCanvas.InkPresenter.StrokeContainer.GetStrokes().ElementAt(s));
                //    }
                //}
                foreach (var s in animation.inkStrokesId)
                {
                    // check if stroke still exists
                    var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s);
                    if (stroke != null && inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Contains(stroke))
                    {
                        inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s).Selected = true;

                        strokesToAnimate.Add(inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(s));
                    }

      
                }

                if (strokesToAnimate.Count == 0)
                {
                    // we can delete this current animation entry
                    animations.GetAnimations().Remove(animation);
                    canvas.Children.Remove(animation.GetPolyline());
                    continue;
                }

                var delta = animation.startPoint;

                var pline = animation.GetPolyline();
                pline.Opacity = 1;

                //canvas.Children.Add(animation.GetPolyline());
                Rect currentPosition = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));


                inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(animation.startPoint.X - (currentPosition.X + currentPosition.Width / 2), animation.startPoint.Y - (currentPosition.Y + currentPosition.Height / 2)));

                // want something here so we reset the location of ink to where it should start from
                // MoveStroke doesn't move it to a position relative to the canvas but rather relative to its current location!

                var i = -1;
                foreach (Point pt in animation.GetPolyline().Points)
                {
                    //container.MoveSelected(new Point(pt.X - prevX, pt.Y - prevY));
                    var r = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(pt.X - delta.X, pt.Y - delta.Y));
                    delta = pt;
                    await Task.Delay(TimeSpan.FromSeconds(0.001));
                    i++;

                }

                if (resetCheckbox.IsChecked == true)
                {
                    currentPosition = inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));


                    inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(allAnimations[0].startPoint.X - (currentPosition.X + currentPosition.Width / 2), allAnimations[0].startPoint.Y - (currentPosition.Y + currentPosition.Height / 2)));

                }

                if (AnimationMode.IsChecked == true)
                {
                    pline.Opacity = 0.3;

                }
                else
                {
                    pline.Opacity = 0;
                }
            }
        }

        //replay selected animation
        private async void Replay(object sender, RoutedEventArgs e)
        {
            FrameworkElement b = sender as FrameworkElement;
            Animation a = b.DataContext as Animation;
            int index = a.id;
            Debug.WriteLine("works:" + a);
            var replayAnimation = animations.GetAnimationAt(index); // won't work once we start deleting

             
            await Animate(replayAnimation, resetCheckbox.IsChecked == true);
               
        }

        private  void DeleteAnimation(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            Animation a = b.DataContext as Animation;
            int index = a.id;
            Debug.WriteLine("works:" + index);
            canvas.Children.Remove(animations.GetAnimationAt(index).GetPolyline());
            animations.RemoveAnimation(index);
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

        private void ToggleAnimationMode(object sender, RoutedEventArgs e)
        {
            isAnimationMode = !isAnimationMode;
            if (isAnimationMode)
            {
                col3.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                col3.Height = new GridLength(0);
            }
        }

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
                var collection = animations.GetAnimations();
                collection[collection.IndexOf(nameChange)] = nameChange;
            }
        }

        private void RenameAnimation(String newName, Animation animation)
        {
            animation.setName(newName);
        }

        private void UserInputTextChanged(object sender, RoutedEventArgs e)
        {
            TextBox renameTextbox = (TextBox)sender;
            String userInput = renameTextbox.Text.Trim();

            if (userInput.Length > 0)
            {
                renameDialog.IsPrimaryButtonEnabled = true;
            } else
            {
                renameDialog.IsPrimaryButtonEnabled = false;
            }
        }

    }
}
  