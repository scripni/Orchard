﻿using Orchard.Data.Migration;

namespace Orchard.Email.DataMigrations {
    public class EmailDataMigration : DataMigrationImpl {

        public int Create() {

            SchemaBuilder.CreateTable("SmtpSettingsPartRecord", table => table
                .ContentPartRecord()
                .Column<string>("Address")
                .Column<string>("Host")
                .Column<int>("Port")
                .Column<bool>("EnableSsl")
                .Column<bool>("RequireCredentials")
                .Column<string>("UserName")
                .Column<string>("Password")
                );

            return 1;
        }
    }
}