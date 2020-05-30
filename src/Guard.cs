using System;

static class Guard
{
    public static void AgainstNull(string argumentName, object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstNullAndWhiteSpace(string argumentName, string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Is empty or only whitespace", argumentName);
        }
    }
}