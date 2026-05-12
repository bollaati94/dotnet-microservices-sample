namespace Common.Models;

public enum EntryType { Arrive, Exit }
public enum PersonType { Employee, Guest }

public record GateEvent(
    Guid EventId,
    string GateId,
    string PersonId,
    PersonType PersonType,
    EntryType EntryType,
    DateTime Timestamp
);
