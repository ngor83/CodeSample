namespace BBWT.Services.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;            
    using System.Linq;
    using System.Security.Principal;

    using BBWT.Data.Localization;
    using BBWT.Data.Menu;
    using BBWT.Domain;
    using BBWT.Services.Interfaces;

    /// <summary>
    /// Service which helps to see and manage Menu Items
    /// </summary>
    public class MenuService : IMenuService
    {
        private const string ConfigFallbackLanguage = "FallbackLanguage";

        private readonly IDataContext context;

        private readonly IPrincipal user;

        /// <summary>Constructs Menu Service object</summary>
        /// <param name="ctx">Data Context</param>
        /// <param name="user">Current User</param>
        public MenuService(IDataContext ctx, IPrincipal user)
        {
            this.context = ctx;
            this.user = user;
        }

        /// <summary>
        /// All menu items
        /// </summary>
        /// <param name="language">language id</param>
        /// <returns>List of menu items</returns>
        public IQueryable<MenuItemPresentation> GetAllMenuItems(string language)
        {
            string defaultLanguage = ConfigurationManager.AppSettings[ConfigFallbackLanguage];

            return this.context.MenuItems.Select(m => new MenuItemPresentation
            {
                Id = m.Id,
                Name = m.Name.Translations.Any(t => t.Language.Id == language) ? m.Name.Translations.FirstOrDefault(t => t.Language.Id == language).Text :
                    m.Name.Translations.FirstOrDefault(t => t.Language.Id == defaultLanguage).Text,
                Order = m.Order,
                Url = m.Url,
                ParentId = m.ParentId,
                IsAdded = false
            });
        }

        /// <summary>
        /// Effective menu items sorted by order
        /// </summary>
        /// <param name="id">Parent id of menu item</param>
        /// <returns>List of menu items</returns>
        public IQueryable<MenuItem> GetMenuByParentId(int id = 0)
        {
            return this.context.MenuItems.Where(m => m.ParentId == id).OrderBy(m => m.Order);
        }

        /// <summary>
        /// Save menu
        /// </summary>
        /// <param name="items">menu items</param>
        /// <param name="language">language id</param>
        /// <returns>result</returns>
        public bool SaveMenu(IList<MenuItemPresentation> items, string language)
        {
            //// remove non-exastant menu items with translations
            foreach (var item in this.context.MenuItems)
            {
                if (!items.Any(i => i.Id == item.Id && !i.IsAdded))
                {
                    this.context.TranslationSets.Remove(item.Name);
                    this.context.MenuItems.Remove(item);
                }
            }

            //// add new items
            var languageEntity = this.context.Languages.First(l => l.Id == language);
            string defaultLanguageId = ConfigurationManager.AppSettings[ConfigFallbackLanguage];
            var defaultLanguage = this.context.Languages.FirstOrDefault(l => l.Id == defaultLanguageId);

            foreach (var item in items.Where(i => i.IsAdded))
            {
                this.context.MenuItems.Add(new MenuItem
                {
                    Id = item.Id,
                    Name = new TranslationSet 
                    { 
                        //// for new items set default value like the current language value
                        Translations = languageEntity.Id == defaultLanguage.Id ? 
                            new List<Translation> 
                            { 
                                new Translation { Text = item.Name, Language = languageEntity, ImportedOn = DateTimeOffset.UtcNow }
                            } 
                            :
                            new List<Translation> 
                            { 
                                new Translation { Text = item.Name, Language = languageEntity, ImportedOn = DateTimeOffset.UtcNow }, 
                                new Translation { Text = item.Name, Language = defaultLanguage, ImportedOn = DateTimeOffset.UtcNow }
                            },
                        CreatedOn = DateTimeOffset.UtcNow
                    },
                    Order = item.Order,
                    Url = item.Url,
                    ParentId = item.ParentId
                });
            }

            // update other items
            foreach (var item in items.Where(i => !i.IsAdded))
            {
                var menuItem = this.context.MenuItems.FirstOrDefault(m => m.Id == item.Id);
                if (menuItem != null)
                {
                    menuItem.Order = item.Order;
                    menuItem.ParentId = item.ParentId;
                    menuItem.Url = item.Url;
                    var currentLanguageTranslation = menuItem.Name.Translations.FirstOrDefault(t => t.Language == languageEntity);
                    if (currentLanguageTranslation == null)
                    {
                        currentLanguageTranslation = new Translation
                        {
                            Language = languageEntity
                        };
                    }

                    currentLanguageTranslation.Text = item.Name;
                    currentLanguageTranslation.ImportedOn = DateTimeOffset.UtcNow;
                }
            }

            this.context.Commit();

            return true;
        }

        /// <summary>
        /// The reset menu.
        /// </summary>
        /// <returns>
        /// result
        /// </returns>
        public bool ResetMenu()
        {            
            foreach (var item in this.context.MenuItems)
            {
                this.context.TranslationSets.Remove(item.Name);
                this.context.MenuItems.Remove(item);
            }

            this.context.Commit();

            this.SeedMenu(this.context);

            return true;
        }

        private void SeedMenu(IDataContext context)
        {
            var language = context.Languages.First(l => l.Id == "en-gb");

            var menuData = new List<MenuItem> 
            {
                new MenuItem { Id = 100, Name = CreateLocalizedValue("New / Modified People", language), Url = "/", Order = 10 },
                new MenuItem { Id = 200, Name = CreateLocalizedValue("People", language),  Order = 20 },
                new MenuItem { Id = 210, Name = CreateLocalizedValue("People Search", language), ParentId=200, Url = "/people", Order = 10 },
                new MenuItem { Id = 300, Name = CreateLocalizedValue("Training Job Titles", language),  Order = 30 },
                new MenuItem { Id = 310, Name = CreateLocalizedValue("Manage Training Job Titles", language), ParentId=300, Url = "/jobtitles", Order = 10 },
                new MenuItem { Id = 400, Name = CreateLocalizedValue("Courses", language),  Order = 40 },
                new MenuItem { Id = 410, Name = CreateLocalizedValue("Manage Courses", language), ParentId=400, Url="/courses",  Order = 10 },
                new MenuItem { Id = 500, Name = CreateLocalizedValue("Cards", language),  Order = 50 },
                new MenuItem { Id = 510, Name = CreateLocalizedValue("Manage Cards Occupations / Categories", language), ParentId=500, Url = "/card/occupations", Order = 50 },

                new MenuItem { Id = 600, Name = this.CreateLocalizedValue("Admin", language), Order = 60 },
                new MenuItem { Id = 610, Name = this.CreateLocalizedValue("System Configuration", language), Url = "/admin/settings", ParentId = 600, Order = 10 },
                new MenuItem { Id = 620, Name = this.CreateLocalizedValue("Manage Companies", language), Url = "/admin/companies", ParentId = 600, Order = 20 },
                new MenuItem { Id = 630, Name = this.CreateLocalizedValue("Manage Email Templates", language), Url = "/admin/templates", ParentId = 600, Order = 30 },
                new MenuItem { Id = 640, Name = this.CreateLocalizedValue("Manage Users", language), Url = "/admin/users", ParentId = 600, Order = 40 },
                new MenuItem { Id = 650, Name = this.CreateLocalizedValue("Manage Groups", language), Url = "/admin/groups", ParentId = 600, Order = 50 },
                new MenuItem { Id = 660, Name = this.CreateLocalizedValue("Manage Roles", language), Url = "/admin/roles", ParentId = 600, Order = 60 },
                new MenuItem { Id = 670, Name = this.CreateLocalizedValue("Manage Permissions", language), Url = "/admin/permissions", ParentId = 600, Order = 70 },                
                new MenuItem { Id = 680, Name = this.CreateLocalizedValue("Manage Menu", language), Url = "/admin/menu", ParentId = 600, Order = 80 },
                new MenuItem { Id = 690, Name = this.CreateLocalizedValue("View Route Access", language), Url = "/admin/routes", ParentId = 600, Order = 90 },
                new MenuItem { Id = 695, Name = this.CreateLocalizedValue("Manage Languages", language), Url = "/admin/languages", ParentId = 600, Order = 95 },

                new MenuItem { Id = 700, Name = this.CreateLocalizedValue("Reports", language), Order = 70 },
                new MenuItem { Id = 710, Name = this.CreateLocalizedValue("Reports list", language), Url = "/reports/index", ParentId = 700, Order = 10 }
            };

            foreach (var menuItem in menuData.Where(menuItem => !context.MenuItems.Any(it => it.Id == menuItem.Id)).OrderBy(it => it.Id))
            {
                context.MenuItems.Add(menuItem);
                context.Commit();
            }
        }

        private TranslationSet CreateLocalizedValue(string value, Language language)
        {
            TranslationSet translationSet = new TranslationSet { Translations = new List<Translation>(), CreatedOn = DateTimeOffset.UtcNow };
            translationSet.Translations.Add(new Translation { Language = language, Text = value, ImportedOn = DateTimeOffset.UtcNow });
            return translationSet;
        }
    }
}