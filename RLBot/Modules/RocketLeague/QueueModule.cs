using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using RLBot.Models;

namespace RLBot.Modules.RocketLeague
{
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        static List<RLQueue> queues = new List<RLQueue>();
        static Random rnd = new Random();
        readonly string DM = "The queue cannot be used in a DM.";
        readonly string NOT_OPEN = "There is no open queue atm. Type \"" + RLBot.COMMAND_PREFIX + "qopen\", to start a new one.";
        readonly string NOT_ENOUGH_PLAYERS = "Not enough players have joined the queue yet! {0}/6";


        [Command("qinfo")]
        [Alias("qi", "qhelp", "qh")]
        [Summary("Shows a list of all the queue commands and how to use them.")]
        public async Task QueueInfoAsync()
        {
            string message = "```commands:\n" +
                             "- new queue: !qopen or !qo\n" +
                             "- join queue: !qjoin or !qj\n" +
                             "- leave queue: !qleave or !ql\n" +
                             "- status of the queue: !qstatus or !qs\n" +
                             "- reset queue (clears queue): !qreset or !qr\n" +
                             "- pick teams from queue (min. 6, clears queue): !qpick or !qp\n" +
                             "- pick captains from queue (min. 2, clears queue): !qcaptain or !qc" +
                             "```";
            
            var dm_channel = await Context.Message.Author.GetOrCreateDMChannelAsync();
            try
            {
                await dm_channel.SendMessageAsync(message);
            }
            catch(HttpException ex)
            when(ex.DiscordCode == 50007)
            {
                // send message normally if dm's are blocked by receiver
                await ReplyAsync(message);
            }
        }

        [Command("qopen")]
        [Alias("qo")]
        [Summary("Create a new queue from which two 3man teams will be picked.")]
        public async Task OpenQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                queue = new RLQueue();
                queue.created = DateTime.Now;
                queue.channel = Context.Channel as SocketGuildChannel;
                queues.Add(queue);

                await ReplyAsync("The queue is open. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.COMMAND_PREFIX + "qjoin\", to join it.");                
        }

        [Command("qjoin")]
        [Alias("qj")]
        [Summary("Join the queue for 6man games.")]
        public async Task JoinQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
            }
            else
            {
                if (queue.users.Count < 6)
                {
                    if (queue.users.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault() == null)
                    {
                        queue.users.Add(Context.Message.Author);
                        await ReplyAsync($"{Context.Message.Author.Mention} joined the queue.");
                    }
                    else
                    {
                        await ReplyAsync("You've already joined the queue.");
                    }
                }
                else
                {
                    await ReplyAsync("The queue is full.");
                }
            }
        }

        [Command("qleave")]
        [Alias("ql")]
        [Summary("Leave the queue for 6man games.")]
        public async Task LeaveQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync("There is no active queue.");
            }
            else
            {
                var user = queue.users.Where(x => x.Id == Context.Message.Author.Id).FirstOrDefault();
                if (user != null)
                {
                    queue.users.Remove(user);
                    await ReplyAsync($"{Context.Message.Author.Mention} left the queue.");
                }
                else
                {
                    await ReplyAsync("You're not in the current queue.");
                }
            }
        }

        [Command("qstatus")]
        [Alias("qs")]
        [Summary("Show a list of all the people in the queue.")]
        public async Task ListOfPlayersInQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
                return;
            }

            if (queue.users.Count == 0)
            {
                await ReplyAsync("There current queue is empty.");
                return;
            }

            string users = string.Join(", ", queue.users);
            await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle("Current queue")
                        .WithDescription(users)
                        .Build());
        }

        [Command("qreset")]
        [Alias("qr")]
        [Summary("Reset the queue.")]
        public async Task ResetQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
            }
            else
            {
                queue.users.Clear();
                queues.Remove(queue);
                await ReplyAsync("The queue has been reset!");
            }
        }

        [Command("qpick")]
        [Alias("qp")]
        [Summary("Pick 6 random players from the queue and divide them into 2 teams.")]
        public async Task PickTeamsFromQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
            }
            else
            {
                // remove offline users from the queue
                queue.users.RemoveAll(x => x.Status == UserStatus.Offline);

                if (queue.users.Count == 6)
                {
                    List<SocketUser> team_a = new List<SocketUser>();
                    List<SocketUser> team_b = new List<SocketUser>();
                    for (int i = 0; i < 6; i++)
                    {
                        int rng = rnd.Next(0, queue.users.Count);
                        if (i % 2 == 0)
                        {
                            team_a.Add(queue.users[rng]);
                        }
                        else
                        {
                            team_b.Add(queue.users[rng]);
                        }
                        queue.users.Remove(queue.users[rng]);
                    }
                    queue.users.Clear();
                    queues.Remove(queue);

                    await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle("Inhouse 3v3 teams")
                        .AddInlineField("Team A", $"{team_a[0].Mention}\n{team_a[1].Mention}\n{team_a[2].Mention}")
                        .AddInlineField("Team B", $"{team_b[0].Mention}\n{team_b[1].Mention}\n{team_b[2].Mention}")
                        .Build());
                }
                else
                {
                    await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.users.Count));
                }
            }
        }

        [Command("qcaptain")]
        [Alias("qc")]
        [Summary("Pick 2 random captains from the queue.")]
        public async Task PickCaptainsFromQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync(DM);
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();
            if (queue == null)
            {
                await ReplyAsync(NOT_OPEN);
            }
            else
            {
                // remove offline users from the queue
                queue.users.RemoveAll(x => x.Status == UserStatus.Offline);

                if (queue.users.Count == 6)
                {
                    List<SocketUser> captains = new List<SocketUser>();

                    for (int i = 0; i < 2; i++)
                    {
                        int rng = rnd.Next(0, queue.users.Count);
                        captains.Add(queue.users[rng]);
                        queue.users.Remove(queue.users[rng]);
                    }

                    await ReplyAsync("", embed: new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle("Inhouse captains")
                        .AddInlineField("Captain A", captains[0].Mention)
                        .AddInlineField("Captain B", captains[1].Mention)
                        .AddField("Remaining", string.Join(", ", queue.users.Select(x => x.Mention)))
                        .Build());

                    queue.users.Clear();
                    queues.Remove(queue);
                }
                else
                {
                    await ReplyAsync(string.Format(NOT_ENOUGH_PLAYERS, queue.users.Count));
                }
            }
        }
    }
}