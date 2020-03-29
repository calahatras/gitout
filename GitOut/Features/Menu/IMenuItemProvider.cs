using System.Collections.Generic;
using GitOut.Features.Navigation;

namespace GitOut.Features.Menu
{
    public interface IMenuItemProvider
    {
        IEnumerable<MenuItem> GetMenuItems(INavigationService navigation);
    }
}
