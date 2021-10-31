using System.Threading.Tasks;
using Godot;
using ThousandYearsHome.Extensions.Exceptions;

namespace ThousandYearsHome.Extensions
{
    public static class NodeExtensions
    {
        public static async Task ToSignalWithArg<T>(
            this Node node,
            Object source,
            string signalName,
            int argIndex,
            T argToWaitFor,
            int signalsToCheck = 3)
        {
            int signalsChecked = 0;
            while (signalsChecked < signalsToCheck)
            {
                object[] args = await node.ToSignal(source, signalName);
                if (args.Length - 1 >= argIndex)
                {
                    object objArg = args[argIndex];
                    if (objArg is T arg && arg.Equals(argToWaitFor))
                    {
                        return;
                    }
                }
                signalsChecked++;
            }

            throw new SignalAwaitCountExceeded($"Signal with the given args didn't occur within {signalsToCheck} signals received.");
        }
    }
}
