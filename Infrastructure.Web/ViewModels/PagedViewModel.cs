using System.Collections.Generic;

namespace Infrastructure.Web.ViewModels
{
    public class PagedViewModel<TModel>
    {
        public IEnumerable<TModel> Items { get; set; }
        public int TotalRows { get; set; }
        public int TotalPages { get; set; }
        public int PageIndex { get; set; }
        public int PageNum { get; set; }
        public int PageSize { get; set; }
    }
}