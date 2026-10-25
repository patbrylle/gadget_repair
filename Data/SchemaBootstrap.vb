Imports Microsoft.EntityFrameworkCore

Namespace Data

    Public Module SchemaBootstrap

        Private ensured As Boolean = False

        ' creates the two support tables when they are missing (safe to call many times)
        Public Sub EnsureSupportTables(db As ApplicationDbContext)

            If ensured Then Return

            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS `AppSettings` (" &
                "`SettingKey` VARCHAR(100) NOT NULL, " &
                "`SettingValue` VARCHAR(1000) NOT NULL DEFAULT '', " &
                "PRIMARY KEY (`SettingKey`)) CHARACTER SET utf8mb4;")

            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS `Notifications` (" &
                "`NotificationId` INT NOT NULL AUTO_INCREMENT, " &
                "`Audience` VARCHAR(20) NOT NULL, " &
                "`TechnicianId` INT NULL, " &
                "`Category` VARCHAR(30) NOT NULL, " &
                "`Title` VARCHAR(150) NOT NULL, " &
                "`Message` VARCHAR(500) NOT NULL, " &
                "`RefKey` VARCHAR(100) NOT NULL, " &
                "`IsRead` TINYINT(1) NOT NULL DEFAULT 0, " &
                "`IsDismissed` TINYINT(1) NOT NULL DEFAULT 0, " &
                "`CreatedAt` DATETIME(6) NOT NULL, " &
                "PRIMARY KEY (`NotificationId`), " &
                "INDEX `IX_Notifications_Audience` (`Audience`, `TechnicianId`)) " &
                "CHARACTER SET utf8mb4;")

            ensured = True

        End Sub

    End Module

End Namespace