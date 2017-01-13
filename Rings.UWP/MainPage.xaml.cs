using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Rings.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            composition = new MediaComposition();
            timeList = new List<TimeModel>() {
                new TimeModel() {Text = "1s", Type = TimeType.Second },
                new TimeModel() {Text = "0.1s", Type = TimeType.HundredMillisecond },
                new TimeModel() {Text = "0.01s", Type = TimeType.TenMillisecond },
                new TimeModel() {Text = "0.001s", Type = TimeType.Millisecond },
            };
            saveModelList = new List<RingToneSaveModel>() {
                new RingToneSaveModel() { Text = "保存为铃声" ,Type = RingToneSaveType.SaveAsTone },
                new RingToneSaveModel() { Text = "仅保存" ,Type = RingToneSaveType.SaveOnly },
            };
            StartPointComboBox.ItemsSource = EndPointComboBox.ItemsSource = timeList;
            SaveTypeView.ItemsSource = saveModelList;
            SaveTypeView.SelectedIndex = StartPointComboBox.SelectedIndex = EndPointComboBox.SelectedIndex = 0;
            RangeSliderView.ValueChanged += RangeSliderView_ValueChanged;
            MediaEle.MediaOpened += MediaEle_MediaOpened;
        }

        private List<TimeModel> timeList;
        private List<RingToneSaveModel> saveModelList;
        private MediaComposition composition;

        public string Tips
        {
            get { return (string)GetValue(TipsProperty); }
            set { SetValue(TipsProperty, value); }
        }

        private bool _IsSaving { get; set; }
        public bool IsSaving
        {
            set
            {
                if (value != _IsSaving)
                {
                    _IsSaving = value;
                    Mask.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            get { return _IsSaving; }
        }
        
        public static readonly DependencyProperty TipsProperty =
            DependencyProperty.Register(nameof(Tips), typeof(string), typeof(MainPage), new PropertyMetadata(null));


        private void OpenPicker_Click(object sender, RoutedEventArgs e)
        {
            SetList();
        }

        private async void SetList()
        {

            var list = await GetFileList();
            if (list != null)
            {
                FileListView.ItemsSource = list;
            }
        }

        private async Task<IReadOnlyList<StorageFile>> GetFileList()
        {
            IReadOnlyList<StorageFile> fileList = null;
            StorageFolder floder = await SelectFolder();
            if (floder != null)
            {
                var list = await floder.GetFilesAsync();
                if (list != null)
                    fileList = list.Where(x => x.ContentType.StartsWith("audio")).ToList();
            }
            return fileList;
        }

        public static async Task<StorageFolder> SelectFolder()
        {
            FolderPicker Picker = new FolderPicker();
            Picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            Picker.FileTypeFilter.Add(".");
            StorageFolder Folder = await Picker.PickSingleFolderAsync();
            return Folder;
        }

        private void MediaEle_MediaOpened(object sender, RoutedEventArgs e)
        {
            RangeSliderView.Maximum = MediaEle.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void RangeSliderView_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (MediaEle.CanPause &&
                (RangeSliderView.IsDragging || RangeSliderView.Value <= RangeSliderView.RangeMin || RangeSliderView.Value >= RangeSliderView.RangeMax))
                MediaEle.Pause();
        }

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var file = e.ClickedItem as StorageFile;
            var stream = await file.OpenAsync(FileAccessMode.Read);
            MediaEle.SetSource(stream, file.ContentType);
            RangeSliderView.Value = RangeSliderView.RangeMin = 0;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (MediaEle.CurrentState)
                {
                    case MediaElementState.Playing:
                        MediaEle.Pause();
                        break;
                    default:
                        MediaEle.Play();
                        break;
                }
            }
            catch (Exception ex)
            {
                Tips = ex.Message;
            }
        }

        private void RelayButton_Click(object sender, RoutedEventArgs e)
        {
            RangeSliderView.Value = RangeSliderView.RangeMin;
            MediaEle.Play();
        }

        private void RangeChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ele = sender as ContentControl;
                if (ele != null && ele.DataContext != null)
                {
                    double value = 0;
                    TimeModel model = null;
                    string content = ele.Content.ToString();
                    switch (ele.DataContext.ToString())
                    {
                        case "StartPoint":
                            model = StartPointComboBox.SelectedItem as TimeModel;
                            if (model != null) value = model.Value;
                            if (content == "-") value = -value;
                            if (RangeSliderView.RangeMin + value > RangeSliderView.Maximum)
                                RangeSliderView.RangeMin = RangeSliderView.Maximum;
                            else if (RangeSliderView.RangeMin + value < RangeSliderView.Minimum)
                                RangeSliderView.RangeMin = RangeSliderView.Minimum;
                            else
                                RangeSliderView.RangeMin += value;
                            break;
                        case "EndPoint":
                            model = EndPointComboBox.SelectedItem as TimeModel;
                            if (model != null) value = model.Value;
                            if (content == "-") value = -value;
                            if (RangeSliderView.RangeMax + value > RangeSliderView.Maximum)
                                RangeSliderView.RangeMax = RangeSliderView.Maximum;
                            else if (RangeSliderView.RangeMax + value < RangeSliderView.Minimum)
                                RangeSliderView.RangeMax = RangeSliderView.Minimum;
                            else
                                RangeSliderView.RangeMax += value;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Tips = ex.Message;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Tips = "";
            Save();
        }

        private async void Save()
        {
            try
            {
                var file = FileListView.SelectedItem as StorageFile;
                if (file == null) throw new Exception("未选择文件或文件无效");
                if (RangeSliderView.RangeMin == RangeSliderView.RangeMax) throw new Exception("请选择范围");
                composition.Clips.Clear();
                var encodingProfile = await MediaEncodingProfile.CreateFromFileAsync(file);
                var clip = await MediaClip.CreateFromFileAsync(file);
                clip.TrimTimeFromStart = TimeSpan.FromSeconds(RangeSliderView.RangeMin);
                clip.TrimTimeFromEnd = TimeSpan.FromSeconds(RangeSliderView.Maximum - RangeSliderView.RangeMax);
                composition.Clips.Add(clip);

                var filename = Path.GetFileNameWithoutExtension(file.Name);
                var ext = Path.GetExtension(file.Name);

                StorageFile saveFile = null;
                var model = SaveTypeView.SelectedItem as RingToneSaveModel;
                if (model == null) throw new Exception("请选择保存类型");
                var saveType = model.Type;
                switch (saveType)
                {
                    case RingToneSaveType.SaveAsTone:
                        saveFile = await KnownFolders.MusicLibrary.CreateFileAsync(filename + ".ring.uwp" + ext, CreationCollisionOption.ReplaceExisting);
                        break;
                    case RingToneSaveType.SaveOnly:
                        filename += DateTime.Now.ToString("_yyyyMMddHHmmss");
                        var picker = new FileSavePicker();
                        picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                        picker.FileTypeChoices.Add("音频文件", new List<string>() { ext });
                        picker.SuggestedFileName = filename;
                        saveFile = await picker.PickSaveFileAsync();
                        break;
                    default: throw new Exception("没有符合的保存类型");
                }

                if (saveFile != null)
                {
                    IsSaving = true;
                    CachedFileManager.DeferUpdates(saveFile);
                    //empty the file
                    await FileIO.WriteBytesAsync(saveFile, new byte[] { });
                    //Save to file using original encoding profile
                    //encodingProfile
                    var result = await composition.RenderToFileAsync(saveFile, MediaTrimmingPreference.Precise);

                    if (result == Windows.Media.Transcoding.TranscodeFailureReason.None)
                    {
                        switch (saveType)
                        {
                            case RingToneSaveType.SaveOnly:
                                Tips = ("保存成功");
                                break;
                            case RingToneSaveType.SaveAsTone:
                                await SaveAsTone(saveFile);
                                await saveFile.DeleteAsync();
                                break;
                        }
                    }
                    else
                    {
                        Tips = ("保存失败:" + result);
                    }
                }
            }
            catch (Exception ex)
            {
                Tips = ex.Message;
            }
            if(IsSaving)
                IsSaving = false;
        }

        private async Task SaveAsTone(StorageFile file)
        {
            var prop = await file.GetBasicPropertiesAsync();
            double size = (double)prop.Size / 1024 / 1024;
            if (size > 1)
            {
                size = Math.Round(size, 2);
                var yesOrNo = await ShowMessageYesOrNo($"该文件大小为{size}M,大于1M的文件可能无法设置为铃声", "是否继续设置");
                if (!yesOrNo) return;
            }
            LauncherOptions options = new LauncherOptions();
            options.TargetApplicationPackageFamilyName = "Microsoft.Tonepicker_8wekyb3d8bbwe";

            ValueSet inputData = new ValueSet() {
                { "Action", "SaveRingtone" },
                { "ToneFileSharingToken", SharedStorageAccessManager.AddFile(file) }
            };

            LaunchUriResult result = await Launcher.LaunchUriForResultsAsync(new Uri("ms-tonepicker:"), options, inputData);

            if (result.Status == LaunchUriStatus.Success)
            {
                Int32 resultCode = (Int32)result.Result["Result"];

                if (resultCode == 0)
                {
                    // no issues
                }
                else
                {
                    switch (resultCode)
                    {
                        case 2:
                            // The specified file was invalid
                            break;
                        case 3:
                            // The specified file's content type is invalid
                            break;
                        case 4:
                            // The specified file was too big
                            break;
                        case 5:
                            // The specified file was too long
                            break;
                        case 6:
                            // The file was protected by DRM
                            break;
                        case 7:
                            // The specified parameter was incorrect
                            break;
                    }
                }
            }
        }

        public async Task<bool> ShowMessageYesOrNo(string str, string title=null)
        {
            var dialog = new ContentDialog()
            {
                Title = title,
                Content = str,
                FullSizeDesired = false,
                PrimaryButtonText = "是",
                SecondaryButtonText = "否"
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) return true;
            return false;
        }

        private async void ShowMessage(string message)
        {
            await new MessageDialog(message).ShowAsync();
        }

        private async void ShowMessageAsync(string message)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    ShowMessage(message);
                }
                );
        }
    }

    public class RingToneSaveModel
    {
        public string Text { get; set; }
        public RingToneSaveType Type { get; set; }
    }

    public class TimeModel
    {
        public string Text { get; set; }
        public double Value
        {
            get
            {
                var value = (double)Offset;
                switch (Type)
                {
                    case TimeType.HundredMillisecond:
                        value /= 10;
                        break;
                    case TimeType.TenMillisecond:
                        value /= 100;
                        break;
                    case TimeType.Millisecond:
                        value /= 1000;
                        break;
                }
                return value;
            }
        }
        public int Offset { get; set; } = 1;
        public TimeType Type { get; set; }
    }

    public enum TimeType
    {
        Second,
        HundredMillisecond,
        TenMillisecond,
        Millisecond,
    }

    public enum RingToneSaveType
    {
        SaveOnly,
        SaveAsTone,
    }

    #region Converter
    class TimeToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((TimeSpan)value).TotalSeconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.FromSeconds((double)value);
        }
    }

    class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.FromSeconds((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((TimeSpan)value).TotalSeconds;
        }
    }

    class TimeStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((TimeSpan)value).ToString("hh\\:mm\\:ss\\.ff");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.Parse((string)value);
        }
    }

    class PlayContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((MediaElementState)value) == MediaElementState.Playing ? "暂停" : "播放";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
    #endregion
}
