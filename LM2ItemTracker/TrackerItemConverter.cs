using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LM2ItemTracker
{
    public class TrackerItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TrackerItem).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            var imagePaths = jo["ImagePaths"];

            TrackerItem item;
            if (imagePaths != null)
                item = new UpgradableTrackerItem();
            else
                item = new TrackerItem();

            serializer.Populate(jo.CreateReader(), item);
            return item;
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
