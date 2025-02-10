namespace ParrotFlintBot.Shared;

public enum ProjectStatus
{
    NotTracked = 0,
    Live = 1,
    Funded = 2,
    Completed = 3,
    LatePledge = 4,
    Undefined = -1,
    Canceled = -2,
}