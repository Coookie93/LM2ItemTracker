using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Net.Sockets;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace LM2ItemTracker
{
    public enum ItemType
    {
        Weapon,
        Use,
        Equip,
        Other,
        Software,
        Mantra
    }

    public class MainViewModel : BindableBase
    {
        private const int port = 56789;
        private const int bufferSize = 1024;

        private readonly byte[] buffer = new byte[bufferSize];
        private readonly Dictionary<int, TrackerItem> items;
        private readonly Dictionary<int, TrackerItem> guardiansDict;
        private readonly object syncObject = new object();

        private TcpClient client;

        private ObservableCollection<TrackerItem> recent;
        public ObservableCollection<TrackerItem> Recent {
            get => recent;
            set => Set(ref recent, value);
        }

        private ObservableCollection<TrackerItem> weapons;
        public ObservableCollection<TrackerItem> Weapons {
            get => weapons;
            set => Set(ref weapons, value);
        }

        private ObservableCollection<TrackerItem> useItems;
        public ObservableCollection<TrackerItem> UseItems {
            get => useItems;
            set => Set(ref useItems, value);
        }

        private ObservableCollection<TrackerItem> equippable;
        public ObservableCollection<TrackerItem> Equippable {
            get => equippable;
            set => Set(ref equippable, value);
        }

        private ObservableCollection<TrackerItem> otherItems;
        public ObservableCollection<TrackerItem> OtherItems {
            get => otherItems;
            set => Set(ref otherItems, value);
        }

        private ObservableCollection<TrackerItem> software;
        public ObservableCollection<TrackerItem> Software {
            get => software;
            set => Set(ref software, value);
        }

        private ObservableCollection<TrackerItem> mantras;
        public ObservableCollection<TrackerItem> Mantras {
            get => mantras;
            set => Set(ref mantras, value);
        }

        private ObservableCollection<TrackerItem> guardians;
        public ObservableCollection<TrackerItem> Guardians {
            get => guardians;
            set => Set(ref guardians, value);
        }

        private string title;
        public string Title {
            get => title;
            set => Set(ref title, value);
        }

        private int windowWidth;
        public int WindowWidth {
            get => windowWidth;
            set => Set(ref windowWidth, value);
        }

        private int windowHeight;
        public int WindowHeight {
            get => windowHeight;
            set => Set(ref windowHeight, value);
        }

        private bool alwaysOnTop;
        public bool AlwaysOnTop {
            get => alwaysOnTop;
            set => Set(ref alwaysOnTop, value);
        }

        private bool showRecent;
        public bool ShowRecent {
            get => showRecent;
            set => Set(ref showRecent, value);
        }

        private bool silhouetteMode;
        public bool SilhouetteMode {
            get => silhouetteMode;
            set => Set(ref silhouetteMode, value);
        }

        private bool editMode;
        public bool EditMode {
            get => editMode;
            set {
                Set(ref editMode, value);
                if (!value)
                    SaveLayoutSettings();
            }
        }

        private bool isConnected;
        public bool IsConnected {
            get => isConnected;
            set => Set(ref isConnected, value);
        }

        private bool isResetting;
        public bool IsResetting {
            get => isResetting;
            set => Set(ref isResetting, value);
        }

        private SolidColorBrush backgroundColour;
        public SolidColorBrush BackgroundColour {
            get => backgroundColour;
            set => Set(ref backgroundColour, value);
        }

        private Color mantraTextColour;
        public Color MantraTextColour {
            get => mantraTextColour;
            set => Set(ref mantraTextColour, value);
        }

        private Color itemFillColour;
        public Color ItemFillColour {
            get => itemFillColour;
            set => Set(ref itemFillColour, value);
        }

        private ICommand connectCommand;
        public ICommand ConnectCommand {
            get {
                if (connectCommand == null)
                    connectCommand = new RelayCommand((x) => !IsConnected, (x) => StartClient());

                return connectCommand;
            }
        }

        private ICommand saveCommand;
        public ICommand SaveCommand {
            get {
                if (saveCommand == null)
                    saveCommand = new RelayCommand((x) => !EditMode, (x) => SaveSettings());

                return saveCommand;
            }
        }

        private ICommand updateItemVisibilityCommand;
        public ICommand UpdateItemVisibilityCommand {
            get {
                if (updateItemVisibilityCommand == null)
                    updateItemVisibilityCommand = new RelayCommand((x) => EditMode, (x) => UpdateItemVisibility(x));

                return updateItemVisibilityCommand;
            }
        }

        private ICommand setBackgroundColourCommand;
        public ICommand SetBackgroundColourCommand {
            get {
                if (setBackgroundColourCommand == null)
                    setBackgroundColourCommand = new RelayCommand((x) => true, (x) => SetBackGroundColour());

                return setBackgroundColourCommand;
            }
        }

        private ICommand setMantraTextColourCommand;
        public ICommand SetMantraTextColourCommand {
            get {
                if (setMantraTextColourCommand == null)
                    setMantraTextColourCommand = new RelayCommand((x) => true, (x) => SetMantraTextColour());

                return setMantraTextColourCommand;
            }
        }

        private ICommand setItemFillColourCommand;
        public ICommand SetItemFillColourCommand {
            get {
                if (setItemFillColourCommand == null)
                    setItemFillColourCommand = new RelayCommand((x) => true, (x) => SetItemFillColour());

                return setItemFillColourCommand;
            }
        }

        public MainViewModel()
        {
            Title = "LM2 Item Tracker";
            WindowWidth = Properties.Settings.Default.WindowWidth;
            WindowHeight = Properties.Settings.Default.WindowHeight;
            AlwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
            ShowRecent = Properties.Settings.Default.ShowRecent;
            SilhouetteMode = Properties.Settings.Default.SilhouetteMode;
            BackgroundColour = (SolidColorBrush)(new BrushConverter().ConvertFrom(Properties.Settings.Default.BackgroundColour));
            MantraTextColour = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.MantraTextColour);
            ItemFillColour = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ItemFillColour);

            Recent = new ObservableCollection<TrackerItem>();
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            { 
                BindingOperations.EnableCollectionSynchronization(Recent, syncObject); 
            }));

            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("LM2ItemTracker.Items.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new TrackerItemConverter());
                items = (Dictionary<int, TrackerItem>)serializer.Deserialize(sr, typeof(Dictionary<int, TrackerItem>));
            }

            Weapons = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Value.ItemType == ItemType.Weapon).Select(kvp => kvp.Value));
            foreach (var pair in Weapons.Zip(Properties.Settings.Default.WeaponsLayout, Tuple.Create))
                pair.Item1.IsVisible = pair.Item2;

            UseItems = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Value.ItemType == ItemType.Use).Select(kvp => kvp.Value));
            foreach (var pair in UseItems.Zip(Properties.Settings.Default.UseItemLayout, Tuple.Create))
                pair.Item1.IsVisible = pair.Item2;

            Equippable = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Value.ItemType == ItemType.Equip).Select(kvp => kvp.Value));
            foreach (var pair in Equippable.Zip(Properties.Settings.Default.EquipItemLayout, Tuple.Create))
                pair.Item1.IsVisible = pair.Item2;

            OtherItems = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Key < 101 && kvp.Value.ItemType == ItemType.Other).Select(kvp => kvp.Value));
            foreach (var pair in OtherItems.Zip(Properties.Settings.Default.OtherItemLayout, Tuple.Create))
                pair.Item1.IsVisible = pair.Item2;

            Software = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Value.ItemType == ItemType.Software).Select(kvp => kvp.Value));
            foreach (var pair in Software.Zip(Properties.Settings.Default.SoftwareLayout, Tuple.Create))
                pair.Item1.IsVisible = pair.Item2;

            Mantras = new ObservableCollection<TrackerItem>(items.Where(kvp => kvp.Value.ItemType == ItemType.Mantra).Select(kvp => kvp.Value));

            using ( StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("LM2ItemTracker.Guardians.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                guardiansDict = (Dictionary<int, TrackerItem>)serializer.Deserialize(sr, typeof(Dictionary<int, TrackerItem>));
            }
            Guardians = new ObservableCollection<TrackerItem>(guardiansDict.Values);
        }

        public void UpdateItemVisibility(object parameter)
        {
            TrackerItem item = (TrackerItem)parameter;
            item.IsVisible = !item.IsVisible;
        }

        public void SetBackGroundColour()
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                System.Drawing.Color newColour = colorDialog.Color;
                BackgroundColour = new SolidColorBrush(Color.FromArgb(newColour.A, newColour.R, newColour.G, newColour.B));
            }
        }

        public void SetMantraTextColour()
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                System.Drawing.Color newColour = colorDialog.Color;
                MantraTextColour = Color.FromArgb(newColour.A, newColour.R, newColour.G, newColour.B);
            }
        }

        public void SetItemFillColour()
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                System.Drawing.Color newColour = colorDialog.Color;
                ItemFillColour = Color.FromArgb(newColour.A, newColour.R, newColour.G, newColour.B);
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.WindowWidth = WindowWidth;
            Properties.Settings.Default.WindowHeight = WindowHeight;
            Properties.Settings.Default.AlwaysOnTop = AlwaysOnTop;
            Properties.Settings.Default.ShowRecent = ShowRecent;
            Properties.Settings.Default.SilhouetteMode = SilhouetteMode;
            Properties.Settings.Default.BackgroundColour = BackgroundColour.Color.ToString();
            Properties.Settings.Default.MantraTextColour = MantraTextColour.ToString();
            Properties.Settings.Default.ItemFillColour = ItemFillColour.ToString();
            Properties.Settings.Default.Save();
        }

        private void SaveLayoutSettings()
        {
            Properties.Settings.Default.WeaponsLayout = Weapons.Select(x => x.IsVisible).ToArray();
            Properties.Settings.Default.UseItemLayout = UseItems.Select(x => x.IsVisible).ToArray();
            Properties.Settings.Default.EquipItemLayout = Equippable.Select(x => x.IsVisible).ToArray();
            Properties.Settings.Default.OtherItemLayout = OtherItems.Select(x => x.IsVisible).ToArray();
            Properties.Settings.Default.SoftwareLayout = Software.Select(x => x.IsVisible).ToArray();
            Properties.Settings.Default.Save();
        }

        private void UpdateRecent(string name, string imagePath)
        {
            lock (syncObject)
            {
                Recent.Insert(0, new TrackerItem()
                {
                    ImagePath = imagePath,
                    Name = name
                });

                if (Recent.Count > 8)
                    Recent.RemoveAt(8);
            }
        }

        private void ClearRecent()
        {
            lock (syncObject)
            {
                Recent.Clear();
            }
        }

        private  void StartClient()
        {
            try
            {
                client = new TcpClient();
                client.BeginConnect(IPAddress.Loopback, port, OnConnected, null);
                IsConnected = true;
            }
            catch (Exception)
            {
                //TODO: maybe add logging
                IsConnected = false;
            }
        }

        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                client.GetStream().BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }
            catch (Exception)
            {
                //TODO: maybe add logging
                IsConnected = false;
            }
        }

        private void OnRead(IAsyncResult ar)
        {
            try
            {
                int bytesRead = client.GetStream().EndRead(ar);
                if (bytesRead <= 0)
                {
                    client.Close();
                    IsConnected = false;
                    return;
                }

                if (bytesRead % 3 == 0)
                {
                    for (int i = 0; i < bytesRead; i += 3)
                    {
                        int sheet = buffer[i + 0];
                        int flag = buffer[i + 1];
                        int data = buffer[i + 2];
                        if (sheet == 2)
                        {
                            if (items.TryGetValue(flag, out TrackerItem item))
                            {
                                int prevCount = item.Count;

                                switch (flag)
                                {
                                    case 3:
                                    {
                                        if (data > 1 && data < 7)
                                            item.Upgrade(1);
                                        else if (data == 7)
                                            item.Upgrade(2);
                                        break;
                                    }
                                    case 5:
                                    {
                                        item.Upgrade(data - 1);
                                        break;
                                    }
                                    case 8:
                                    case 76:
                                    {
                                        item.Count = data;
                                        break;
                                    }
                                    case 9:
                                    {
                                        item.Upgrade(data > 1 ? 1 : 0);
                                        break;
                                    }
                                    case 62:
                                    case 75:
                                    {
                                        item.Upgrade(data - 1);
                                        item.Count = data;
                                        break;
                                    }
                                }

                                if (!IsResetting)
                                {
                                    if (item.IsCollected)
                                    {
                                        if ((flag == 62 || flag == 75 || flag == 76) && prevCount < item.Count)
                                        {
                                            UpdateRecent(item.Name, item.ImagePath);
                                        }
                                        else if (flag == 8)
                                        {
                                            UpdateRecent(item.Name, item.ImagePath);
                                        }
                                        else if (flag == 84)
                                        {
                                            UpdateRecent(item.Name, item.ImagePath);
                                        }
                                    }
                                    else
                                    {
                                        UpdateRecent(item.Name, item.ImagePath);
                                    }
                                }

                                item.IsCollected = data > 0;
                            }
                        }
                        else if (sheet == 3)
                        {
                            if (guardiansDict.TryGetValue(flag, out TrackerItem item))
                                item.IsCollected = true;
                        }
                        else if (sheet == 100)
                        {
                            IsResetting = true;
                            foreach (TrackerItem item in items.Values)
                                item.Reset();

                            foreach (TrackerItem item in guardiansDict.Values)
                                item.Reset();
                        }
                        else if (sheet == 101)
                        {
                            IsResetting = false;
                        }
                        else if (sheet == 102)
                        {
                            IsResetting = false;

                            foreach (TrackerItem item in items.Values)
                                item.Reset();

                            foreach (TrackerItem item in guardiansDict.Values)
                                item.Reset();

                            ClearRecent();
                        }
                    }
                }

                client.GetStream().BeginRead(buffer, 0, buffer.Length, OnRead, null);
            }
            catch (Exception)
            {
                //TODO: maybe add logging
                IsConnected = false;
            }
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
}
