using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using ImageTools;
using ImageTools.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media;
using Naboo.AppUtil;
using Microsoft.Phone.Tasks;

namespace Naboo.MitbbsReader.Pages
{
    public partial class ImageViewerPage : PhoneApplicationPage
    {
        private String _url;
        private String _localPath;

        public ImageViewerPage()
        {
            InitializeComponent();

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.InitAdControl(AdGrid);

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"];
            }
            else
            {
                _url = null;
            }

            if (parameters.ContainsKey("LocalPath"))
            {
                _localPath = parameters["LocalPath"];
            }
            else
            {
                _localPath = null;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PageHelper.CleanupAdControl(AdGrid);

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_localPath != null)
                {
                    ImageBrowser.Navigate(new Uri(_localPath, UriKind.Relative));
                }
                else if (_url != null)
                {
                    ImageBrowser.Source = new Uri(_url, UriKind.Absolute);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开图片！");
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class ImageLoader
    {
        public ObservableCollection<ImageLoaderItem> Items { get; set; }
        public StackPanel DisplayPanel { get; set; }
        public ScrollViewer ScrollPanel { get; set; }

        public Brush FontColor { get; set; }
        public double FontSize { get; set; }

        public bool IsOffline { get; set; }
        public Guid OfflineID { get; set; }
        public bool ShowButtons { get; set; }
        public bool CacheImage { get; set; }
        public bool IsClub { get; set; }

        private PhoneApplicationPage _page;
        public PhoneApplicationPage Page 
        {
            get
            {
                return _page;
            }

            set
            {
                if (value != _page)
                {
                    if (_page != null)
                    {
                        _page.OrientationChanged -= Page_OrientationChanged;
                    }

                    _page = value;
                    _page.OrientationChanged += Page_OrientationChanged;
                }
            }
        }
        
        public WebClient Web { get; set; }
        private Queue<ImageLoaderItem> _loadQueue = new Queue<ImageLoaderItem>();
        private volatile bool _loading = false;
        private bool _lowMemory = false;
        private int _lastUnloaded = 0;
        
        public ImageLoader()
        {
            Items = new ObservableCollection<ImageLoaderItem>();
            FontSize = 0;

            ShowButtons = true;
            CacheImage = true;
            IsClub = false;
        }

        public void ClearImages()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                ImageLoaderItem imageItem = Items[i];
                imageItem.Unload();

                DisplayPanel.Children.Remove(imageItem.ImagePanel);
            }

            Items.Clear();
            _loadQueue.Clear();
            //_loading = false;
        }

        public void StopDownload()
        {
            _loadQueue.Clear();
        }

        public void LoadImage(String imageUrl, String pageUrl, UIElement insertBefore = null, bool handlePinch = true, bool showViewerLink = true, String loadingText = null, String errorText = null)
        {
            ImageLoaderItem imageItem = new ImageLoaderItem();

            Uri uri = new Uri(imageUrl);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            imageItem.LoadingManager = this;
            imageItem.ImageUrl = imageUrl;
            imageItem.ImageFileName = filename;
            imageItem.PageUrl = pageUrl;
            imageItem.HandlePinch = handlePinch;
            imageItem.ShowViewerLink = showViewerLink;
            imageItem.FontColor = FontColor;
            imageItem.FontSize = FontSize;
            imageItem.MaxImageHeight = CalculateMaxImageHeight();
            imageItem.MaxImageWidth = CalculateMaxImageWidth();
            imageItem.NavigationService = Page.NavigationService;

            imageItem.IsOffline = IsOffline;
            imageItem.OfflineID = OfflineID;

            imageItem.ShowButtons = ShowButtons;
            imageItem.CacheImage = CacheImage;
            imageItem.IsClub = IsClub;
            
            if (loadingText != null)
            {
                imageItem.LoadingText = loadingText;
            }

            if (errorText != null)
            {
                imageItem.ErrorText = errorText;
            }

            int controlIndex = -1;

            if (insertBefore != null)
            {
                controlIndex = DisplayPanel.Children.IndexOf(insertBefore);
            }
            else
            {
                controlIndex = DisplayPanel.Children.Count;
            }

            Items.Add(imageItem);

            DisplayPanel.Children.Insert(controlIndex, imageItem.ImagePanel);
            
            AddToQueue(imageItem);
        }

#if NODO
        private long _memoryLimit = 200000000;
        private long _hardMemoryLimit = 270000000;
#else
        private float _memoryPercentageLimit = .66f;
        private float _hardMemoryPercentageLimit = .88f;
#endif

        public void AddToQueue(ImageLoaderItem imageItem)
        {
            if (imageItem != null)
            {
                imageItem.ShowLoading();
                _loadQueue.Enqueue(imageItem);
                DownloadNextInQueue();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void DownloadNextInQueue()
        {
            _lowMemory = false;
            if (!_loading && (_loadQueue.Count > 0))
            {
                while (_loadQueue.Count > 0)
                {
                    ImageLoaderItem imageItem = _loadQueue.Dequeue();

                    if (imageItem != null)
                    {
                        bool hasEnoughMemory = true;
#if NODO
                        long usedMemory = (long)Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage");
                        hasEnoughMemory = (usedMemory < _hardMemoryLimit) && (imageItem.IgnoreMemoryLimit || (usedMemory < _memoryLimit));
#else
                        long memoryLimit = App.Settings.AppMemoryLimit;
                        long usedMemory = Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage;

                        hasEnoughMemory = (usedMemory < memoryLimit * _hardMemoryPercentageLimit) && (imageItem.IgnoreMemoryLimit || usedMemory < memoryLimit * _memoryPercentageLimit);
#endif
                        if (hasEnoughMemory)
                        {
                            _lastUnloaded++;

                            _loading = true;
                            imageItem.ImageItemLoaded += OnImageItemLoaded;
                            imageItem.Load(Web);

                            break;
                        }
                        else
                        {
                            _lowMemory = true;

                            if (!imageItem.IgnoreMemoryLimit || _lastUnloaded > 0)
                            {
                                imageItem.ShowManualLoad();
                            }
                            else
                            {
                                if (imageItem.ShowViewerLink)
                                {
                                    imageItem.ShowViewer();
                                }
                                else
                                {
                                    imageItem.ShowError();
                                }
                            }
                        }
                    }
                }
            }
        }

        public void LoadMoreImages(ImageLoaderItem imageItemToLoad, int imageCount)
        {
            int startIndex = Items.IndexOf(imageItemToLoad);

            if(startIndex >= 0)
            {
                int endIndex = startIndex + imageCount - 1;

                if(endIndex > (Items.Count - 1))
                {
                    endIndex = Items.Count - 1;
                }

                if (_lowMemory)
                {
                    RecycleMemoryForNewImage(startIndex, endIndex, imageCount);
                }

                for (int i = startIndex; i <= endIndex; i++)
                {
                    Items[i].IgnoreMemoryLimit = true;
                    AddToQueue(Items[i]);
                }
            }
        }

        private int GetTotalLoaded()
        {
            int result = 0;
            foreach (ImageLoaderItem item in Items)
            {
                if (item.ImageState == ImageLoaderItem.ImageStateType.Loaded)
                {
                    result++;
                }
            }

            return result;
        }

        public void RecycleMemoryForNewImage(int startIndex, int endIndex, int totalToUnload)
        {
            int frontIndex = 0;
            int backIndex = Items.Count - 1;
            int totalUnloaded = 0;

            while ((totalUnloaded < totalToUnload) && ((frontIndex < startIndex) || (backIndex > endIndex)))
            {
                bool imageUnloaded = false;
                if ((startIndex - frontIndex) >= (backIndex - endIndex))
                {
                    ImageLoaderItem imageItem = Items[frontIndex];

                    if (imageItem.ImageState == ImageLoaderItem.ImageStateType.Loaded)
                    {
                        imageUnloaded = true;
                    }

                    imageItem.Unload();

                    frontIndex++;
                }
                else
                {
                    ImageLoaderItem imageItem = Items[backIndex];

                    if (imageItem.ImageState == ImageLoaderItem.ImageStateType.Loaded)
                    {
                        imageUnloaded = true;
                    }

                    imageItem.Unload();

                    backIndex--;
                }

                if (imageUnloaded)
                {
                    totalUnloaded++;
                }
            }

            _lastUnloaded = totalUnloaded;

            if (totalUnloaded > 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void OnImageItemLoaded(object sender, EventArgs args)
        {
            ImageLoaderItem imageItem = (sender as ImageLoaderItem);
            if (imageItem != null)
            {
                imageItem.ImageItemLoaded -= OnImageItemLoaded;
                imageItem.IgnoreMemoryLimit = false;

                _loading = false;
                DownloadNextInQueue();
            }
        }

        public void Page_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            try
            {
                double maxImageHeight = CalculateMaxImageHeight();
                double maxImageWidth = CalculateMaxImageWidth();

                for (int i = 0; i < Items.Count; i++)
                {
                    ImageLoaderItem imageItem = Items[i];

                    imageItem.MaxImageHeight = maxImageHeight;
                    imageItem.MaxImageWidth = maxImageWidth;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private double CalculateMaxImageHeight()
        {
            if (ScrollPanel == null)
            {
                return double.PositiveInfinity;
            }

            double maxImageHeight = 0;
            if ((Page.Orientation == PageOrientation.Portrait) || (Page.Orientation == PageOrientation.PortraitDown) || (Page.Orientation == PageOrientation.PortraitUp))
            {
                maxImageHeight = DisplayPanel.ActualWidth;
            }
            else
            {
                maxImageHeight = ScrollPanel.ActualHeight - 20;
            }

            return maxImageHeight;
        }

        private double CalculateMaxImageWidth()
        {
            if (ScrollPanel == null)
            {
                return double.PositiveInfinity;
            }

            double maxImageWidth = 0;
            if ((Page.Orientation == PageOrientation.Portrait) || (Page.Orientation == PageOrientation.PortraitDown) || (Page.Orientation == PageOrientation.PortraitUp))
            {
                maxImageWidth = DisplayPanel.ActualWidth;
            }
            else
            {
                maxImageWidth = ScrollPanel.ActualHeight;
            }

            return maxImageWidth;
        }
    }

    public class ImageLoaderItem
    {
        public enum ImageStateType
        {
            Unloaded,
            Loading,
            Loaded,
            Error,
        }

        public ImageLoader LoadingManager { get; set; }
        public String ImageUrl { get; set; }
        public String ImageFileName { get; set; }
        public String PageUrl { get; set; }
        public StackPanel ImagePanel { get; private set; }
        public ImageStateType ImageState { get; private set; }
        public bool HandlePinch { get; set; }
        public bool ShowViewerLink { get; set; }
        public String LoadingText { get; set; }
        public String ManualLoadText { get; set; }
        public String ErrorText { get; set; }
        public NavigationService NavigationService { get; set; }
        public bool IgnoreMemoryLimit { get; set; }
        public bool ShowButtons { get; set; }
        public bool CacheImage { get; set; }
        public bool IsClub { get; set; }

        public Brush FontColor { get; set; }
        public double FontSize { get; set; }

        public bool IsOffline { get; set; }
        public Guid OfflineID { get; set; }

        public event EventHandler<EventArgs> ImageItemLoaded;

        private WebClient _web;

        private BitmapImage _bitmap = null;
        private Image _imageControl = null;
        private AnimatedImage _extImageControl = null;
        private MemoryStream _dupStream = null;

        private double _imageHeight = double.PositiveInfinity;
        public double ImageHeight
        {
            get
            {
                return _imageHeight;
            }

            set
            {
                _imageHeight = value;
                if (_imageHeight < _maxImageHeight)
                {
                    _maxImageHeight = _imageHeight;
                }

                AdjustMaxHeight();
            }
        }

        private double _imageWidth = double.PositiveInfinity;
        public double ImageWidth
        {
            get
            {
                return _imageWidth;
            }

            set
            {
                _imageWidth = value;
                if (_imageWidth < _maxImageWidth)
                {
                    _maxImageWidth = _imageWidth;
                }

                AdjustMaxHeight();
            }
        }


        private double _maxImageHeight = double.PositiveInfinity;
        public double MaxImageHeight
        {
            get
            {
                return _maxImageHeight;
            }

            set
            {
                if (value <= ImageHeight)
                {
                    _maxImageHeight = value;

                    if (_imageControl != null)
                    {
                        _imageControl.MaxHeight = _maxImageHeight;
                        _imageControl.Width = double.NaN;
                    }
                    else if (_extImageControl != null)
                    {
                        _extImageControl.MaxHeight = _maxImageHeight;
                        _extImageControl.Width = double.NaN;
                    }
                }
            }
        }

        private double _maxImageWidth = double.PositiveInfinity;
        public double MaxImageWidth
        {
            get
            {
                return _maxImageWidth;
            }

            set
            {
                if (value <= ImageWidth)
                {
                    _maxImageWidth = value;

                    if (_imageControl != null)
                    {
                        _imageControl.MaxWidth = _maxImageWidth;
                        _imageControl.Width = double.NaN;
                    }
                    else if (_extImageControl != null)
                    {
                        _extImageControl.MaxWidth = _maxImageWidth;
                        _extImageControl.Width = double.NaN;
                    }
                }
            }
        }

        private void AdjustMaxHeight()
        {
            if (_imageHeight > 0 && _imageHeight < double.PositiveInfinity && _imageWidth > 0 && _imageWidth < double.PositiveInfinity)
            {
                if (_imageWidth * _maxImageHeight / _imageHeight < 300)
                {
                    _maxImageHeight = _imageHeight * 300 / _imageWidth;
                    if (_maxImageHeight > _imageHeight)
                    {
                        _maxImageHeight = _imageHeight;
                    }
                }
            }
        }

        public ImageLoaderItem()
        {
            HandlePinch = false;
            ImageState = ImageStateType.Unloaded;
            ImagePanel = new StackPanel();

            LoadingText = "<正在读取图片...>";
            ManualLoadText = "<点击显示图片>";
            ErrorText = "<点击打开图片>";

            IgnoreMemoryLimit = false;
            ShowButtons = true;
            CacheImage = true;
            IsClub = false;
        }

        public void ShowLoading()
        {
            ImagePanel.Children.Clear();

            TextBlock text = new TextBlock() { Text = LoadingText };
            if (FontColor != null)
            {
                text.Foreground = FontColor;
            }

            if (FontSize > 0)
            {
                text.FontSize = FontSize;
            }

            ImagePanel.Children.Add(text);
            ImagePanel.Children.Add(new PerformanceProgressBar() { IsIndeterminate = true, Margin = new Thickness(0, 0, 180, 0) });
        }

        public void ShowManualLoad()
        {
            ImagePanel.Children.Clear();

            HyperlinkButton loadLink = new HyperlinkButton();
            loadLink.SetValue(TiltEffect.IsTiltEnabledProperty, true);
            loadLink.Content = ManualLoadText;
            loadLink.Click += ManualLoadLink_Click;
            loadLink.TargetName = "_new";
            loadLink.DataContext = ImageUrl;
            loadLink.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            loadLink.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;

            GestureListener gl = GestureService.GetGestureListener(loadLink);
            gl.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            if (FontColor != null)
            {
                loadLink.Foreground = FontColor;
            }

            if (FontSize > 0)
            {
                loadLink.FontSize = FontSize;
            }

            ImagePanel.Children.Add(loadLink);
        }

        public void ShowViewer()
        {
            ImagePanel.Children.Clear();

            HyperlinkButton viewerLink = new HyperlinkButton();
            viewerLink.SetValue(TiltEffect.IsTiltEnabledProperty, true);
            viewerLink.Content = ErrorText;
            viewerLink.Click += ImageViewerLink_Click;
            viewerLink.TargetName = "_new";
            viewerLink.DataContext = ImageUrl;
            viewerLink.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            viewerLink.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;

            GestureListener gl = GestureService.GetGestureListener(viewerLink);
            gl.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            if (FontColor != null)
            {
                viewerLink.Foreground = FontColor;
            }

            if (FontSize > 0)
            {
                viewerLink.FontSize = FontSize;
            }

            ImagePanel.Children.Add(viewerLink);
        }

        public void ShowError()
        {
            ImagePanel.Children.Clear();

            ImageState = ImageStateType.Error;

            TextBlock text = new TextBlock() { Text = ErrorText };
            if (FontColor != null)
            {
                text.Foreground = FontColor;
            }

            if (FontSize > 0)
            {
                text.FontSize = FontSize;
            }

            ImagePanel.Children.Add(text);
        }

        public void Unload()
        {
            if (ImageState == ImageStateType.Loaded)
            {
                ImagePanel.Height = ImagePanel.ActualHeight;
            }

            ImagePanel.Children.Clear();

            if (_bitmap != null)
            {
                _bitmap = null;
            }

            if (_imageControl != null)
            {
                _imageControl.Source = null;
                _imageControl = null;
            }

            if (_extImageControl != null)
            {
                _extImageControl.Source = null;
                _extImageControl = null;
            }

            if (_dupStream != null)
            {
                _dupStream.Close();
                _dupStream = null;
            }

            ImageState = ImageStateType.Unloaded;

            ShowManualLoad();
        }

        public void Load(WebClient web = null)
        {
            _web = web;

            if (_web == null)
            {
                _web = new WebClient();
            }

            ShowLoading();

            ImageState = ImageStateType.Loading;

            Stream imageStream;
            if (App.Settings.OfflineContentManager.TryLoadOfflineContent(OfflineID, ImageUrl, out imageStream))
            {

                AsyncCallHelper.DelayCall(
                    () =>
                    {
                        try
                        {
                            LoadImageFromStream(imageStream, true);
                        }
                        finally
                        {
                            imageStream.Close();
                        }
                    }
                    );

                return;
            }
            else if (IsOffline)
            {
                IsOffline = false;
                ShowManualLoad();

                if (ImageItemLoaded != null)
                {
                    ImageItemLoaded(this, null);
                }

                return;
            }
            
            Version osVer = System.Environment.OSVersion.Version;

            if ((osVer.Major > 7) || ((osVer.Major == 7) && (osVer.Minor >= 1)))
            {
                _web.Headers[HttpRequestHeader.Referer] = PageUrl;
            }

            try
            {
                _web.OpenReadCompleted += OnImageLoaded;
                _web.OpenReadAsync(new Uri(ImageUrl, UriKind.Absolute));
            }
            catch (Exception e)
            {
                LoadImageFromStream(null);
            }
        }

        private void CreateButtons()
        {
            if (!ShowButtons)
            {
                return;
            }

            Grid expandButtonPanel = new Grid();
            expandButtonPanel.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            expandButtonPanel.HorizontalAlignment = HorizontalAlignment.Right;

            Button expandButton = new Button();
            ImageBrush expandButtonImage = new ImageBrush();
            expandButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/overflowdots.png", UriKind.Relative));
            expandButtonImage.Stretch = Stretch.None;
            expandButton.Style = (Style)Application.Current.Resources["ImageButton"];
            expandButton.Content = expandButtonImage;
            expandButton.Padding = new Thickness(0);
            expandButton.BorderThickness = new Thickness(0);
            expandButton.Margin = new Thickness(0);
            expandButton.Width = 93;
            expandButton.Height = 54;

            GestureListener gl = GestureService.GetGestureListener(expandButton);
            gl.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            expandButton.Click += ExpandButton_Click;

            expandButtonPanel.Children.Add(expandButton);

            StackPanel buttonPanel = new StackPanel();
            buttonPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            buttonPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

            Border buttonBorder = new Border();
            buttonBorder.BorderBrush = (Brush)App.Current.Resources["PhoneAccentBrush"];
            buttonBorder.BorderThickness = new Thickness(2);
            buttonBorder.CornerRadius = new CornerRadius(3);
            buttonBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            buttonBorder.Visibility = Visibility.Collapsed;
            buttonBorder.Child = buttonPanel;

            expandButton.DataContext = buttonBorder;

            int buttonSize = 72;

            Button openButton = new Button();
            ImageBrush openButtonImage = new ImageBrush();
            openButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/zoomin_roundbutton.png", UriKind.Relative));
            openButtonImage.Stretch = Stretch.None;
            openButton.Style = (Style)Application.Current.Resources["ImageButton"];
            openButton.Content = openButtonImage;
            openButton.Padding = new Thickness(0);
            openButton.BorderThickness = new Thickness(0);
            openButton.Margin = new Thickness(0);
            openButton.Width = buttonSize;
            openButton.Height = buttonSize;

            GestureListener gl1 = GestureService.GetGestureListener(openButton);
            gl1.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            openButton.Click += OpenButton_Click;
            openButton.DataContext = this;
            buttonPanel.Children.Add(openButton);

            if (_bitmap != null)
            {
                Button saveButton = new Button();
                ImageBrush saveButtonImage = new ImageBrush();
                saveButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/save_roundbutton.png", UriKind.Relative));
                saveButtonImage.Stretch = Stretch.None;
                saveButton.Style = (Style)Application.Current.Resources["ImageButton"];
                saveButton.Content = saveButtonImage;
                saveButton.Padding = new Thickness(0);
                saveButton.BorderThickness = new Thickness(0);
                saveButton.Margin = new Thickness(0);
                saveButton.Width = buttonSize;
                saveButton.Height = buttonSize;

                GestureListener gl2 = GestureService.GetGestureListener(saveButton);
                gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

                saveButton.Click += SaveButton_Click;
                saveButton.DataContext = this;
                buttonPanel.Children.Add(saveButton);
            }

            Button shareButton = new Button();
            ImageBrush shareButtonImage = new ImageBrush();
            shareButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/people_roundbutton.png", UriKind.Relative));
            shareButtonImage.Stretch = Stretch.None;
            shareButton.Style = (Style)Application.Current.Resources["ImageButton"];
            shareButton.Content = shareButtonImage;
            shareButton.Padding = new Thickness(0);
            shareButton.BorderThickness = new Thickness(0);
            shareButton.Margin = new Thickness(0);
            shareButton.Width = buttonSize;
            shareButton.Height = buttonSize;

            GestureListener gl3 = GestureService.GetGestureListener(shareButton);
            gl3.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            shareButton.Click += ShareButton_Click;
            shareButton.DataContext = this;
            buttonPanel.Children.Add(shareButton);

            ImagePanel.Children.Add(expandButtonPanel);
            ImagePanel.Children.Add(buttonBorder);
        }

        private void LoadImageFromStream(Stream imageStream, bool isCached = false)
        {
            bool imageLoaded = false;
            ImagePanel.Children.Clear();
            _bitmap = null;
            _imageControl = null;
            _extImageControl = null;

            if (_dupStream != null)
            {
                _dupStream.Close();
                _dupStream = null;
            }

            if (imageStream != null)
            {
                try
                {
                    Image imageControl = new System.Windows.Controls.Image();

                    _bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    _bitmap.SetSource(imageStream);
                    imageControl.Source = _bitmap;
                    imageControl.DataContext = this;

                    if (_bitmap.PixelHeight > 0)
                    {
                        ImageHeight = _bitmap.PixelHeight;
                    }

                    if (_bitmap.PixelWidth > 0)
                    {
                        ImageWidth = _bitmap.PixelWidth;
                    }

                    imageControl.MaxHeight = MaxImageHeight;
                    imageControl.MaxWidth = MaxImageWidth;

                    imageControl.DataContext = this;

                    CreateButtons();

                    ImagePanel.Children.Add(imageControl);
                    _imageControl = imageControl;

                    if (HandlePinch)
                    {
                        AddGestureHandler(imageControl);
                    }

                    ImagePanel.Height = double.NaN;
                    imageLoaded = true;
                    ImageState = ImageStateType.Loaded;
                }
                catch (Exception)
                {
                    imageLoaded = false;
                    _bitmap = null;
                    _imageControl = null;
                }

                if (!imageLoaded && (imageStream.Length > 200))
                {
                    try
                    {
                        imageStream.Seek(0, System.IO.SeekOrigin.Begin);

                        _dupStream = new MemoryStream();

                        imageStream.CopyTo(_dupStream);
                        _dupStream.Seek(0, System.IO.SeekOrigin.Begin);
                        
                        ImageTools.Controls.AnimatedImage extImageControl = new ImageTools.Controls.AnimatedImage();

                        ImageTools.ExtendedImage image = new ImageTools.ExtendedImage();
                        image.SetSource(_dupStream);
                        extImageControl.Source = image;
                        extImageControl.DataContext = this;

                        if (image.PixelHeight > 0)
                        {
                            ImageHeight = image.PixelHeight;
                        }

                        if (image.PixelWidth > 0)
                        {
                            ImageWidth = image.PixelWidth;
                        }

                        extImageControl.MaxHeight = MaxImageHeight;
                        extImageControl.MaxWidth = MaxImageWidth;

                        extImageControl.DataContext = this;

                        CreateButtons();

                        ImagePanel.Children.Add(extImageControl);
                        _extImageControl = extImageControl;

                        if (HandlePinch)
                        {
                            AddGestureHandler(extImageControl);
                        }

                        ImagePanel.Height = double.NaN;
                        imageLoaded = true;
                        ImageState = ImageStateType.Loaded;
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            if (!imageLoaded)
            {
                if (isCached)
                {
                    imageStream.Close();
                    App.Settings.OfflineContentManager.CleanupOfflineContent(OfflineID, ImageUrl);

                    IsOffline = false;
                    ImageState = ImageStateType.Unloaded;
                    ShowManualLoad();

                    _web = null;

                    if (ImageItemLoaded != null)
                    {
                        ImageItemLoaded(this, null);
                    }
                }
                else
                {
                    ImageState = ImageStateType.Error;

                    if (ShowViewerLink)
                    {
                        ShowViewer();
                    }
                    else
                    {
                        ShowError();
                    }
                }
            }

            _web = null;

            if (ImageItemLoaded != null)
            {
                ImageItemLoaded(this, null);
            }

            if (imageLoaded && !isCached && imageStream != null && CacheImage)
            {
                imageStream.Seek(0, System.IO.SeekOrigin.Begin);
                App.Settings.OfflineContentManager.SaveOfflineContent(OfflineID, ImageUrl, imageStream);
            }
        }

        private void OnImageLoaded(object sender, OpenReadCompletedEventArgs args)
        {
            if (_web != null)
            {
                _web.OpenReadCompleted -= OnImageLoaded;
            }

            if (args.Error == null)
            {
                LoadImageFromStream(args.Result);
            }
            else
            {
                LoadImageFromStream(null);
            }
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            if (IsClub || (IsOffline && App.Settings.OfflineContentManager.OfflineContentExists(OfflineID, ImageUrl)))
            {
                String pageUrl = String.Format(
                    "/Pages/ImageViewerPage.xaml?LocalPath={0}",
                    Uri.EscapeDataString(App.Settings.OfflineContentManager.GenerateOfflineFileName(OfflineID, ImageUrl).Replace('\\', '/'))
                    );

                NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
            }
            else
            {
#if NODO
                String pageUrl = String.Format("/Pages/ImageViewerPage.xaml?Url={0}", Uri.EscapeDataString(ImageUrl));
                NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
#else
            
                PageHelper.OpenGeneralLink(ImageUrl, NavigationService);
#endif
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveImage();
        }

        private void ShareButton_Click(object sender, EventArgs e)
        {
            String title;
            if (!String.IsNullOrEmpty(ImageFileName))
            {
                title = "分享图片: " + ImageFileName;
            }
            else
            {
                title = "分享图片";
            }

            ShareLinkTask shareTask = new ShareLinkTask()
            {
                Title = title,
                Message = "Shared from OneTap MITBBS for Windows Phone",
                LinkUri = new Uri(ImageUrl)
            };

            shareTask.Show();

            App.Track("Navigation", "OpenPage", "SocialSharePicture"); 
        }

        private void ExpandButton_Click(object sender, EventArgs e)
        {
            if ((sender as Button).DataContext != null)
            {
                UIElement buttonPanel = ((sender as Button).DataContext as UIElement);

                if (buttonPanel.Visibility == Visibility.Visible)
                {
                    buttonPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    buttonPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void ManualLoadLink_Click(object sender, RoutedEventArgs e)
        {
            if (LoadingManager != null)
            {
                LoadingManager.LoadMoreImages(this, 20);
            }

            App.Settings.SetIgnoreTap();
        }

        private void ImageViewerLink_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                String imageUrl = (String)(sender as HyperlinkButton).DataContext;

                if (imageUrl != null)
                {
                    bool showImageInsideApp = false;
#if NODO
                    showImageInsideApp = true;
#endif
                    if (showImageInsideApp)
                    {

                        String pageUrl = String.Format("/Pages/ImageViewerPage.xaml?Url={0}", Uri.EscapeDataString(imageUrl));
                        NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                    }
                    else
                    {
                        PageHelper.OpenGeneralLink(imageUrl, NavigationService);
                    }
                }
            }

            App.Settings.SetIgnoreTap();
        }

        private void GeneralButton_Tapped(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            // This tap handler is created to stop propagating the tap event
            //
            e.Handled = true;
        }

        private void AddGestureHandler(System.Windows.UIElement control)
        {
            GestureListener gl = GestureService.GetGestureListener(control);
            gl.PinchStarted += new EventHandler<PinchStartedGestureEventArgs>(Image_PinchStarted);
            gl.PinchDelta += new EventHandler<PinchGestureEventArgs>(Image_PinchDelta);
            gl.DoubleTap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(Image_DoubleTapped);
        }

        private double _maxControlSize = 1918;
        private void Image_DoubleTapped(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            Image imageControl = (sender as Image);
            if (imageControl != null)
            {
                ImageLoaderItem imageLoaderItem = (imageControl.DataContext as ImageLoaderItem);
            
                if (imageControl.ActualWidth < (imageLoaderItem.ImagePanel.ActualWidth - 1))
                {
                    imageControl.MaxHeight = _maxControlSize;
                    imageControl.MaxWidth = double.PositiveInfinity;
                    imageControl.Width = imageLoaderItem.ImagePanel.ActualWidth;
                }
                else
                {
                    imageControl.MaxHeight = MaxImageHeight;
                    imageControl.MaxWidth = MaxImageWidth;
                    imageControl.Width = MaxImageWidth;
                }
            }
            else
            {
                AnimatedImage extImageControl = (sender as AnimatedImage);
                if (extImageControl != null)
                {
                    ImageLoaderItem imageLoaderItem = (extImageControl.DataContext as ImageLoaderItem);
            
                    if (extImageControl.ActualWidth < (imageLoaderItem.ImagePanel.ActualWidth - 1))
                    {
                        extImageControl.MaxHeight = _maxControlSize;
                        extImageControl.MaxWidth = double.PositiveInfinity;
                        extImageControl.Width = imageLoaderItem.ImagePanel.ActualWidth;
                    }
                    else
                    {
                        extImageControl.MaxHeight = MaxImageHeight;
                        extImageControl.MaxWidth = MaxImageWidth;
                        extImageControl.Width = MaxImageWidth;
                    }
                }
            }

            App.Settings.SetIgnoreTap();
        }

        private double _originalImageWidth = 0;
        private void Image_PinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            Image imageControl = (sender as Image);
            if (imageControl != null)
            {
                _originalImageWidth = imageControl.ActualWidth;
            }
            else
            {
                AnimatedImage extImageControl = (sender as AnimatedImage);
                if (extImageControl != null)
                {
                    _originalImageWidth = extImageControl.ActualWidth;
                }
            }
        }

        private void Image_PinchDelta(object sender, PinchGestureEventArgs e)
        {
            Image imageControl = (sender as Image);
            if (imageControl != null)
            {
                ImageLoaderItem imageLoaderItem = (imageControl.DataContext as ImageLoaderItem);

                imageControl.MaxHeight = _maxControlSize;
                imageControl.MaxWidth = Double.PositiveInfinity;
                double newWidth = _originalImageWidth * e.DistanceRatio;
                if (newWidth < 50)
                {
                    newWidth = 50;
                }

                if (newWidth > imageLoaderItem.ImagePanel.ActualWidth)
                {
                    newWidth = imageLoaderItem.ImagePanel.ActualWidth;
                }
                imageControl.Width = newWidth;
            }
            else
            {
                AnimatedImage extImageControl = (sender as AnimatedImage);
                if (extImageControl != null)
                {
                    ImageLoaderItem imageLoaderItem = (extImageControl.DataContext as ImageLoaderItem);

                    extImageControl.MaxHeight = _maxControlSize;
                    extImageControl.MaxWidth = Double.PositiveInfinity;
                    double newWidth = _originalImageWidth * e.DistanceRatio;
                    if (newWidth < 50)
                    {
                        newWidth = 50;
                    }

                    if (newWidth > imageLoaderItem.ImagePanel.ActualWidth)
                    {
                        newWidth = imageLoaderItem.ImagePanel.ActualWidth;
                    }
                    extImageControl.Width = newWidth;
                }
            }
        }

        private void SaveImage()
        {
            if (_bitmap == null)
            {
                return;
            }

            try
            {
                String tempJPEG = "_onetap_mitbbs_tempJPEG.jpg";

                WriteableBitmap wb = new WriteableBitmap(_bitmap);

                var myStore = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream imageFileSream = myStore.CreateFile(tempJPEG);

                wb.SaveJpeg(imageFileSream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                imageFileSream.Close();

                imageFileSream = myStore.OpenFile(tempJPEG, FileMode.Open, FileAccess.Read);

                MediaLibrary library = new MediaLibrary();

                String imageFilename = "_onetap_mitbbs_saved_picture_" + DateTime.Now.ToFileTimeUtc() + ".jpg";
                Picture pic = library.SavePicture(imageFilename, imageFileSream);

                imageFileSream.Close();

                MessageBox.Show("此图片已保存至'Saved Pictures'");
            }
            catch (Exception)
            {
                MessageBox.Show("存储图像文件失败");
            }
        }
    }
}