using System.Collections.Generic;

namespace LM2ItemTracker
{
    public class UpgradableTrackerItem : TrackerItem
    {
        public List<string> ImagePaths = new List<string>();

        public override void Upgrade(int level)
        {
            ImagePath = ImagePaths[level];
        }

        public override void Reset()
        {
            Count = 0;
            IsCollected = false;
            ImagePath = ImagePaths[0];
        }
    }
}
