using System.Collections.Generic;
using ConnectApp.Models.Model;

namespace ConnectApp.Models.ViewModel {
    public class MyEventsScreenViewModel {
        public List<IEvent> futureEventsList;
        public List<IEvent> pastEventsList;
        public bool futureListLoading;
        public bool pastListLoading;
        public int futureEventTotal;
        public int pastEventTotal;
        public Dictionary<string, Place> placeDict;
    }
}