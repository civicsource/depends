create database Dependencies
GO

create schema dep
GO

CREATE TABLE dep.Applications
(ID int IDENTITY(1, 1) PRIMARY KEY,
Name varchar(100) NOT NULL
)
GO

CREATE TABLE dep.Controllers
(ID int IDENTITY(1, 1) PRIMARY KEY,
Name varchar(500) NOT NULL,
RequiresTaxAuthorityContext bit NULL,
AppID int NOT NULL,
FOREIGN KEY (AppID) references dep.Applications (ID)
)
GO

CREATE TABLE dep.Actions
(ID int IDENTITY (1, 1) PRIMARY KEY,
Name varchar(100) NOT NULL,
[Type] varchar(25) NULL,
ControllerID int NOT NULL,
FOREIGN KEY (ControllerID) references dep.Controllers (ID)
)
GO

CREATE TABLE dep.Views
(ID int IDENTITY (1, 1) PRIMARY KEY,
Name varchar(100) NOT NULL,
[Path] varchar (500) NOT NULL,
AppID int NOT NULL,
FOREIGN KEY (AppID) references dep.Applications (ID)
)
GO

CREATE TABLE dep.[Permissions]
(ID int IDENTITY (1, 1) PRIMARY KEY,
Name varchar(500) NOT NULL,
[Description] varchar(1000) NULL
)
GO

CREATE TABLE dep.ActionPermissions
(ActionID int NOT NULL,
PermissionID int NOT NULL,
FOREIGN KEY (ActionID) references dep.Actions (ID),
FOREIGN KEY (PermissionID) references dep.[Permissions] (ID)
)
GO

CREATE TABLE dep.ControllerPermissions
(ControllerID int NOT NULL,
PermissionID int NOT NULL,
FOREIGN KEY (ControllerID) references dep.Controllers (ID),
FOREIGN KEY (PermissionID) references dep.[Permissions] (ID)
)
GO

CREATE TABLE dep.ViewActions
(ViewID int NOT NULL,
ActionID int NOT NULL,
FOREIGN KEY (ViewID) references dep.Views (ID),
FOREIGN KEY (ActionID) references dep.Actions (ID)
)
GO