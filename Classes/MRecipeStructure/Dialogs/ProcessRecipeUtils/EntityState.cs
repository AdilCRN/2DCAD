namespace MRecipeStructure.Dialogs.ProcessRecipeUtils
{
    public enum EntityState
	{
		ABORTED = -3,
		EMPTY = -2,
		ERROR = -1,
		WAITING = 0,
		RUNNING = 1,
		PAUSED = 2,
		COMPLETED = 3
	}
}
