using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    internal sealed partial class CommandsHelper : BaseObservable
    {

        public static CommandContextItem CreateCyclicCommand(string firstName, Action firstAction, string secondName, Action secondAction, IconInfo firstIcon, IconInfo secondIcon)
        {
            var toggleCommand = new CommandContextItem(firstName)
            {
                Title = firstName,
                Icon = firstIcon,
            };
            toggleCommand.Command = new AnonymousCommand(() =>
            {
                if (toggleCommand.Title == firstName)
                {
                    firstAction();
                    toggleCommand.Title = secondName;
                    toggleCommand.Icon = secondIcon;
                }
                else
                {
                    secondAction();
                    toggleCommand.Title = firstName;
                    toggleCommand.Icon = firstIcon;
                }

            })
            {
                Result = CommandResult.KeepOpen(),
            };

            return toggleCommand;

        }

        /// <summary>
        /// This method is responsible to create a cyclic interaction of commands
        /// <example>
        /// <code>
        /// Donwload -> Cancel -> Download -> ...
        /// </code>
        /// </example>
        /// </summary>
        public static Command CreateDownloadWithCancelCommand(
            Func<CancellationToken, Task> downloadFunc,
            string commandDownloadName,
            string commandCancelName
        )
        {
            var cancellationToken = new CancellationTokenSource();
            Action execute = () => { };

            var command = new AnonymousCommand(() => execute())
            {
                Name = commandDownloadName,
                Result = CommandResult.KeepOpen()
            };

            void SetDownloadCommand()
            {
                command.Name = commandDownloadName;
                execute = () =>
                {
                    _ = ExecuteDownload();
                    SetCancelCommand();
                };
            }

            void SetCancelCommand()
            {
                command.Name = commandCancelName;
                execute = () =>
                {
                    command.Result = CommandResult.Confirm(new ConfirmationArgs()
                    {
                        Title = "CancelDownload".ToLocalized(),
                        Description = "CancelDialogDescription".ToLocalized(),
                        IsPrimaryCommandCritical = true,
                        PrimaryCommand = new AnonymousCommand(() =>
                        {
                            cancellationToken.Cancel();
                            cancellationToken = new CancellationTokenSource();
                            SetDownloadCommand();
                        })
                        {
                            Name = "Confirm".ToLocalized(),
                            Result = CommandResult.KeepOpen(),
                        }
                    });
                };

            }

            async Task ExecuteDownload()
            {
                SetCancelCommand();
                try
                {
                    await downloadFunc(cancellationToken.Token);
                }
                finally
                {
                    SetDownloadCommand();
                }
            }


            SetDownloadCommand();

            return command;
        }
    }
}
