using System;

namespace Application.Exceptions
{
    public class ItemNotFoundException(string message) : Exception(message)
    {

    }
}
