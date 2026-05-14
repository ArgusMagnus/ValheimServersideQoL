namespace Valheim.ServersideQoL;

readonly record struct GlobalKey(string Key)
{
    public GlobalKey(GlobalKeys key) : this(key.ToString().ToLower()) { }

    public static implicit operator string(in GlobalKey key) => key.Key; 
}