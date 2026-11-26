CREATE DATABASE Banco;

GO

USE Banco;

CREATE TABLE Client( --não deixava criar com nome User
	ClientID int IDENTITY not null PRIMARY KEY,
	Name varchar(50) not null,
	IsActive BIT not null DEFAULT 1,
	Region nvarchar (50) not null,
	PasswordHash varchar(255) null,
	IBAN varchar(50) not null UNIQUE,
	Amount DECIMAL(10,2) not null DEFAULT 0,

	CONSTRAINT Check_Client_Amount CHECK (Amount >= 0)
);

CREATE TABLE Host(
	HostID int IDENTITY not null PRIMARY KEY,
	Hostname varchar(50) not null,
	IPAddress varchar(45) not null,
	OSType varchar(15) not null,
	HostStatus BIT NOT NULL DEFAULT 1,
	Timestamp datetime not null DEFAULT SYSUTCDATETIME(),
	Region varchar(50) not null
)

CREATE TABLE BankTransaction(
	TransactionID INT IDENTITY not null PRIMARY KEY,
	SenderIBAN varchar(50) not null,
	ReceiverIBAN varchar(50) not null,
	IPAddress varchar(45) not null,
	Amount DECIMAL (18,2) not null,
	Timestamp datetime not null DEFAULT SYSUTCDATETIME(),

	FOREIGN KEY (SenderIBAN) REFERENCES Client(IBAN),

	CONSTRAINT Check_BankTransaction_Amount CHECK (Amount >= 0)
)

CREATE TABLE SecurityEvent(
	SecurityEventID INT IDENTITY not null PRIMARY KEY,
	HostID INT not null,
	TransactionID INT null,
	EventType varchar(50) not null,
	RawPayload nvarchar(max) not null,
	Timestamp datetime not null DEFAULT SYSUTCDATETIME(),
	IPAddress varchar(45) not null,

	FOREIGN KEY (HostID) REFERENCES Host(HostID),
	FOREIGN KEY (TransactionID) REFERENCES BankTransaction (TransactionID)
)

CREATE TABLE Incident(
	IncidentId INT IDENTITY not null PRIMARY KEY,
	SecurityEventID INT not null,
	Title nvarchar(100) not null,
	IncidentDescription nvarchar(500) null,
	Severity int not null,
	Status nvarchar(20) not null,
	Timestamp datetime not null  DEFAULT SYSUTCDATETIME(),
	DetectionRuleID int not null,

	FOREIGN KEY (SecurityEventID) REFERENCES SecurityEvent(SecurityEventID),

	CONSTRAINT Check_Incident_Severity CHECK (Severity BETWEEN 1 AND 4),
	CONSTRAINT Check_Incident_Status CHECK (Status IN ('New', 'Investigating', 'Resolved'))
)

CREATE TABLE DetectionRule (
    DetectionRuleID INT IDENTITY NOT NULL PRIMARY KEY,
    RuleName VARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EventType VARCHAR(50) NOT NULL,
	Threshold DECIMAL(18,2) NULL, --patamares monetários. e.g.: uma transf. de 5k dava trigger.
    TimeWindowMinutes INT NULL, --quanto tempo teríamos para fechar o ticket
    Severity INT NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_DetectionRule_Severity
        CHECK (Severity BETWEEN 1 AND 4),

    CONSTRAINT CK_DetectionRule_TimeWindow
        CHECK (TimeWindowMinutes IS NULL OR TimeWindowMinutes > 0)
)

ALTER TABLE Incident
ADD CONSTRAINT FK_Incident_DetectionRule
FOREIGN KEY (DetectionRuleID)
REFERENCES DetectionRule(DetectionRuleID);

CREATE INDEX IDX_BankTransaction_Sender
ON BankTransaction(SenderIBAN);

CREATE INDEX IDX_BankTransaction_Receiver
ON BankTransaction(ReceiverIBAN);

CREATE INDEX IDX_SecurityEvent_Host
ON SecurityEvent(HostID);

CREATE INDEX IDX_Incident_Status
ON Incident(Status);


--população das tabelas

INSERT INTO DetectionRule
(RuleName, Description, EventType, Threshold,
 TimeWindowMinutes, Severity, IsEnabled)
VALUES

('Large Transaction',
 'Detects transactions above the configured amount.',
 'TRANSACTION', 5000, NULL, 3, 1),

('High Transaction Frequency',
 'Detects an unusually high number of transactions within a time window.',
 'TRANSACTION_FREQUENCY', 5, 1, 3, 1),

('Unusual Transaction Location',
 'Flags transactions originating from an unexpected location.',
 'LOCATION_CHANGE', NULL, 60, 2, 1),

('Repeated Failed Logins',
 'Detects repeated failed authentication attempts.',
 'FAILED_LOGIN', 5, 10, 4, 1),

('Inactive Host',
 'Detects hosts that have stopped reporting activity.',
 'HOST_INACTIVITY', NULL, 30, 3, 1),

('Large Transfer',
 'Detects transfers above the configured amount.',
 'TRANSFER', 10000, NULL, 4, 1);

 SELECT * FROM DetectionRule


 INSERT INTO Client (Name, IsActive, Region, PasswordHash, IBAN, Amount)
VALUES
('John Smith', 1, 'Portugal', '$2a$04$UKMfHIYA5UxVis9sE/ILL', 'PT50000100000000000000101', 15000.00),
('Maria Costa', 1, 'Portugal', 'OgkdNEqdfwBxzg52ZyTZA6UZ7xYDZ4FK', 'PT50000100000000000000102', 8500.00),
('David Wilson', 1, 'United Kingdom', '$2a$04$GCwNHAKf6CD.UcemDYjWWOpZyZqG0X', 'GB29000000000000000000103', 22000.00),
('Ana Silva', 1, 'Portugal', 'QXin9PvRn3W20OqE/JU0Ode', 'PT50000100000000000000104', 3200.00),
('Robert Brown', 0, 'United States', 'kovpvQ3U6ywNNGN7ezUQWWTcdbZTf3F.Hi', 'US00000000000000000105', 0.00);

INSERT INTO Host (Hostname, IPAddress, OSType, HostStatus, Timestamp, Region)
VALUES
('BANK-SERVER-01', '192.168.1.10', 'Windows Server', 1, '2026-11-20 08:00:00', 'Portugal'),
('BANK-SERVER-02', '192.168.1.11', 'Linux', 1, '2026-11-20 08:05:00', 'Portugal'),
('BANK-ATM-01', '192.168.1.20', 'Linux', 1, '2026-11-20 08:10:00', 'Portugal'),
('BANK-DB-01', '192.168.1.30', 'Windows Server', 1, '2026-11-20 08:15:00', 'Portugal'),
('BANK-OLD-PC', '192.168.1.50', 'Windows 10', 0, '2026-11-19 18:00:00', 'Portugal');

INSERT INTO BankTransaction
(SenderIBAN, ReceiverIBAN, IPAddress, Amount, Timestamp)
VALUES
('PT50000100000000000000101', 'PT50000100000000000000102',
 '192.168.1.10', 250.00, '2026-11-20 09:00:00'),

('PT50000100000000000000102', 'PT50000100000000000000104',
 '192.168.1.11', 100.00, '2026-11-20 09:05:00'),

('GB29000000000000000000103', 'PT50000100000000000000101',
 '203.0.113.25', 7500.00, '2026-11-20 09:10:00'),

('PT50000100000000000000101', 'PT50000100000000000000104',
 '192.168.1.10', 50.00, '2026-11-20 09:12:00'),

('PT50000100000000000000104', 'PT50000100000000000000102',
 '198.51.100.40', 12000.00, '2026-11-20 09:15:00');

 SELECT * FROM SecurityEvent
 SELECT * FROM BankTransaction
 INSERT INTO SecurityEvent
(HostID, TransactionID, EventType, RawPayload, Timestamp, IPAddress)
VALUES
(1, 1, 'TRANSACTION', 
 '{"amount":250,"result":"success"}',
 '2026-11-20 09:00:01', '192.168.1.10'),

(2, 3, 'TRANSACTION',
 '{"amount":7500,"result":"success","location":"Unknown"}',
 '2026-11-20 09:10:01', '203.0.113.25'),

(1, 5, 'TRANSACTION',
 '{"amount":12000,"result":"success"}',
 '2026-11-20 09:15:01', '198.51.100.40'),

(2, NULL, 'FAILED_LOGIN',
 '{"username":"test_user","result":"failed","attempts":5}',
 '2026-11-20 09:20:00', '203.0.113.50'),

(4, NULL, 'HOST_INACTIVITY',
 '{"hostname":"BANK-DB-01","lastHeartbeat":"2026-11-20T08:00:00"}',
 '2026-11-20 09:30:00', '192.168.1.30');


  SELECT * FROM SecurityEvent
  SELECT * FROM Incident

  INSERT INTO Incident
(SecurityEventID, DetectionRuleID, Title, IncidentDescription, Severity, Status, Timestamp)
VALUES
(2, 1, 'Large Transaction',
 'A transaction exceeded the configured amount threshold.',
 3, 'New', '2026-11-20 09:10:02'),

(3, 6, 'Large Transfer',
 'A transfer exceeded the configured transfer threshold.',
 4, 'Investigating', '2026-11-20 09:15:02'),

(4, 4, 'Multiple Failed Logins',
 'Multiple failed login attempts were detected.',
 4, 'New', '2026-11-20 09:20:01'),

(5, 5, 'Inactive Host',
 'The host stopped sending heartbeat events.',
 3, 'Investigating', '2026-11-20 09:30:01');
 
 SELECT * FROM BankTransaction

 SELECT * FROM Client

 SELECT * FROM DetectionRule

 SELECT * FROM Host

 SELECT * FROM Incident

UPDATE DetectionRule SET IsEnabled = 0 WHERE DetectionRuleID = 1;

UPDATE DetectionRule 
SET RuleName = 'High Client Activity (1h)',
    Description = 'Detects a single client making too many transaction requests within 1 hour.',
    EventType = 'CLIENT_HOURLY_LIMIT',
    Threshold = 10
WHERE DetectionRuleID = 3;

SELECT * FROM Incident ORDER BY IncidentId DESC;
SELECT TOP 10 * FROM SecurityEvent ORDER BY SecurityEventID DESC;

INSERT INTO BankTransaction (SenderIBAN, ReceiverIBAN, IPAddress, Amount, Timestamp)
SELECT TOP 10 'PT50000100000000000000101', 'PT50000100000000000000102', '192.168.1.10', 15.00, SYSUTCDATETIME()
FROM sys.objects;

SELECT TOP 1 TransactionID FROM BankTransaction ORDER BY TransactionID DESC;

UPDATE Host 
SET HostStatus = 1, Timestamp = DATEADD(MINUTE, -60, SYSUTCDATETIME()) 
WHERE HostID = 2;

SELECT TOP 12 * FROM Incident ORDER BY IncidentID DESC;
SELECT HostID, Hostname, HostStatus, Timestamp FROM Host WHERE HostID = 2;