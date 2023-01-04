namespace HotelListing.API.Core.Models
{
    public class QueryParameters
    {
        private int _pageSize = 15;
        public int PageNumber { get; set; }
        public int StartIndex { get; set; }

        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = value;
            }

        }
    }
}
