﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CallingMediaBot.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CallingMediaBot.Web.Interfaces;
using CallingMediaBot.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

/// <summary>
/// Helper class for Graph.
/// </summary>
///
/// NOTE: This file will be slowly removed and placed into files similar to CallService
public class GraphHelper : IGraph
{
    private readonly ILogger<GraphHelper> logger;
    private readonly IEnumerable<Web.Options.UserOptions> users;
    private readonly BotOptions botOptions;
    private readonly GraphServiceClient graphServiceClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphHelper"/> class.
    /// </summary>
    /// <param name="httpClientFactory">IHttpClientFactory instance.</param>
    /// <param name="logger">ILogger instance.</param>
    public GraphHelper(ILogger<GraphHelper> logger, IOptions<List<UserOptions>> users, IOptions<BotOptions> botOptions, GraphServiceClient graphServiceClient)
    {
        this.logger = logger;
        this.users = users.Value;
        this.botOptions = botOptions.Value;
        this.graphServiceClient = graphServiceClient;
    }

    /// <inheritdoc/>
    public async Task<OnlineMeeting?> CreateOnlineMeetingAsync()
    {
        try
        {
            var onlineMeeting = new OnlineMeeting
            {
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddMinutes(30),
                Subject = "Calling bot meeting",
            };

            var userId = users.First().Id;

            var onlineMeetingResponse = await graphServiceClient.Users[userId].OnlineMeetings
                       .Request()
                       .AddAsync(onlineMeeting);
            return onlineMeetingResponse;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<Call?> JoinScheduledMeeting(string meetingUrl)
    {
        try
        {
            MeetingInfo meetingInfo;
            ChatInfo chatInfo;

            (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(meetingUrl);

            var call = new Call
            {
                CallbackUri = new Uri(botOptions.BotBaseUrl, "callback").ToString(),
                RequestedModalities = new List<Modality>()
                {
                    Modality.Audio
                },
                MediaConfig = new ServiceHostedMediaConfig
                {
                },
                ChatInfo = chatInfo,
                MeetingInfo = meetingInfo,
                TenantId = (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId()
            };

            var statefulCall = await graphServiceClient.Communications.Calls
                    .Request()
                    .AddAsync(call);

            return statefulCall;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task InviteParticipant(string meetingId)
    {
        try
        {
            var participants = new List<InvitationParticipantInfo>()
            {
                new InvitationParticipantInfo
                {
                    User = new Identity
                    {
                        DisplayName = this.users.ElementAt(1).DisplayName,
                        Id = this.users.ElementAt(1).Id
                    }
                },
                AdditionalData = new Dictionary<string, object>()
                        {
                            DisplayName = this.users.ElementAt(2).DisplayName,
                            Id = this.users.ElementAt(2).Id
                        }
                    }
                }
            };

            var statefulCall = await graphServiceClient.Communications.Calls[meetingId].Participants
                .Invite(participants)
                .Request()
                .PostAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
