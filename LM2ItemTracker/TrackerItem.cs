namespace LM2ItemTracker
{
    public class TrackerItem : BindableBase
    {
        public ItemType ItemType;

        private string name;
        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        private int count;
        public int Count {
            get => count;
            set => Set(ref count, value);
        }

        private string imagePath;
        public string ImagePath {
            get => imagePath;
            set => Set(ref imagePath, value);
        }

        private bool showCount;
        public bool ShowCount {
            get => showCount;
            set => Set(ref showCount, value);
        }

        private bool isVisible;
        public bool IsVisible {
            get => isVisible;
            set => Set(ref isVisible, value);
        }

        private bool isCollected;
        public bool IsCollected {
            get => isCollected;
            set => Set(ref isCollected, value);
        }

        public virtual void Upgrade(int level) { }
        public virtual void Reset()
        {
            Count = 0;
            IsCollected = false;
        }
    }
}
