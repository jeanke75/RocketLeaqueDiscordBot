﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.Models;

namespace RLBot.Modules.RocketLeague
{
    [Group("queue")]
    public class RocketLeagueModule : ModuleBase<SocketCommandContext>
    {
        static List<RLQueue> queues = new List<RLQueue>();
        static Random rnd = new Random();

        [Command("open")]
        [Summary("Open the queue for players to join 6man games.")]
        public async Task OpenQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync("The queue cannot be used in DM.");
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();

            if (queue == null)
            {
                queue = new RLQueue();
                queue.created = DateTime.Now;
                queue.channel = Context.Channel as SocketGuildChannel;
                queues.Add(queue);
            }

            if (!queue.isOpen)
            {
                queue.users.Clear();
                queue.isOpen = true;
                await ReplyAsync("The queue is open. Type \"" + RLBot.PREFIX + "queue join\", to join it.");
            }
            else
                await ReplyAsync("There is already an active queue. Type \"" + RLBot.PREFIX + "queue join\", to join it.");
        }

        [Command("join")]
        [Summary("Join the queue for 6man games.")]
        public async Task JoinQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync("The queue cannot be used in DM.");
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();

            if (queue == null || !queue.isOpen)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.PREFIX + "queue open\", to start a new one.");
            }
            else
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
        }

        [Command("leave")]
        [Summary("Leave the queue for 6man games.")]
        public async Task LeaveQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync("The queue cannot be used in DM.");
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();

            if (queue == null || !queue.isOpen)
            {
                await ReplyAsync("There is no open queue atm.");
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

        [Command("reset")]
        [Summary("Reset the queue.")]
        public async Task ResetQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync("The queue cannot be used in DM.");
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();

            if (queue == null || !queue.isOpen)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.PREFIX + "queue open\", to start a new one.");
            }
            else
            {
                queue.users.Clear();
                queues.Remove(queue);
                await ReplyAsync("The queue has been reset!");
            }
        }

        [Command("pick")]
        [Summary("Pick 6 random players from the queue and divide them into 2 teams.")]
        public async Task PickQueueAsync()
        {
            if (!(Context.Channel is SocketGuildChannel))
            {
                await ReplyAsync("The queue cannot be used in DM.");
                return;
            }

            var queue = queues.Where(x => x.channel == Context.Channel).FirstOrDefault();

            if (queue == null || !queue.isOpen)
            {
                await ReplyAsync("There is no open queue atm. Type \"" + RLBot.PREFIX + "queue open\", to start a new one.");
            }
            else
            {
                // remove all offline players from the queue
                queue.users.RemoveAll(x => x.Status == UserStatus.Offline);

                if (queue.users.Count >= 6)
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

                    var embed = new EmbedBuilder()
                       .WithColor(Color.Default)
                       .WithTitle("Inhouse 3v3 teams")
                       .WithCurrentTimestamp()
                       .AddInlineField("Team A", $"{team_a[0].Mention}\n{team_a[1].Mention}\n{team_a[2].Mention}")
                       .AddInlineField("Team B", $"{team_b[0].Mention}\n{team_b[1].Mention}\n{team_b[2].Mention}");
                    await Context.Channel.SendMessageAsync("", embed: embed.Build());

                    //await ReplyAsync($"Team A: {team_a[0].Mention}, {team_a[1].Mention}, {team_a[2].Mention}  // Team B: {team_b[0].Mention}, {team_b[1].Mention}, {team_b[2].Mention}");
                }
                else
                {
                    await ReplyAsync($"Not enough players have joined the queue yet! {queue.users.Count}/6");
                }
            }
        }
    }
}