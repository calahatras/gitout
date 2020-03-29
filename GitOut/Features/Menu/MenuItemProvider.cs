using System.Collections.Generic;
using System.Linq;
using GitOut.Features.Navigation;

namespace GitOut.Features.Menu
{
    public class MenuItemProvider : IMenuItemProvider, IMenuItemCollection
    {
        private readonly ICollection<MenuItemContext> contexts;

        public MenuItemProvider(
            IEnumerable<MenuItemContext> contexts
        ) => this.contexts = contexts.ToList();

        public void Add(MenuItemContext context) => contexts.Add(context);

        public IEnumerable<MenuItem> GetMenuItems(INavigationService navigation) => contexts.Select(context => new MenuItem
        {
            Name = context.Name,
            Icon = context.Icon,
            Command = context.PageName == null
                ? null
                : new NavigateLocalCommand<object>(
                    navigation,
                    context.PageName
                )
        });
    }
}
