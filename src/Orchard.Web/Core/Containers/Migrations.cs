﻿using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace Orchard.Core.Containers {
    public class Migrations : DataMigrationImpl {
        public int Create() {
            SchemaBuilder.CreateTable("ContainerPartRecord",
                          table => table
                              .ContentPartRecord()
                              .Column<bool>("Paginated")
                              .Column<int>("PageSize")
                              .Column<string>("OrderByProperty")
                              .Column<int>("OrderByDirection"));

            SchemaBuilder.CreateTable("ContainerSettingsPartRecord", table => table
                .ContentPartRecord()
                .Column<int>("DefaultPageSize", column => column.WithDefault(10))
               );

            ContentDefinitionManager.AlterPartDefinition("ContainerPart", builder => builder.Attachable());
            ContentDefinitionManager.AlterPartDefinition("ContainablePart", builder => builder.Attachable());
 
            return 1;
        }

    }
}