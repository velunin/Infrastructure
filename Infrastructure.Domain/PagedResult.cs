using System.Collections.Generic;

namespace Infrastructure.Domain
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Results { get; set; }

        public int PageNum { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalPages
        {
            get
            {
                var pages = TotalRows/PageSize;
                if (TotalRows%PageSize > 0) pages += 1;

                return pages;
            }

        }

        public int TotalRows { get; set; }
    }
}