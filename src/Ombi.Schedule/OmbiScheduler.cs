﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Ombi.Core.Settings;
using Ombi.Schedule.Jobs;
using Ombi.Schedule.Jobs.Couchpotato;
using Ombi.Schedule.Jobs.Emby;
using Ombi.Schedule.Jobs.Lidarr;
using Ombi.Schedule.Jobs.Ombi;
using Ombi.Schedule.Jobs.Plex;
using Ombi.Schedule.Jobs.Plex.Interfaces;
using Ombi.Schedule.Jobs.Radarr;
using Ombi.Schedule.Jobs.SickRage;
using Ombi.Schedule.Jobs.Sonarr;
using Ombi.Settings.Settings.Models;
using Quartz;
using Quartz.Spi;

namespace Ombi.Schedule
{
    public static class OmbiScheduler
    {
        //public void Setup()
        //{
        //    CreateJobDefinitions();
        //}

        //public void CreateJobDefinitions()
        //{
        //    var contentSync = JobBuilder.Create<PlexContentSync>()
        //        .UsingJobData("recentlyAddedSearch", false)
        //        .WithIdentity(nameof(PlexContentSync), "Plex")
        //        .Build();

        //    var recentlyAdded = JobBuilder.Create<PlexContentSync>()
        //        .UsingJobData("recentlyAddedSearch", true)
        //        .WithIdentity("PlexRecentlyAdded", "Plex")
        //        .Build();
        //}

        public static async Task UseQuartz(this IApplicationBuilder app)
        {
            // Job Factory through IOC container
            var jobFactory = (IJobFactory)app.ApplicationServices.GetService(typeof(IJobFactory));
            var service = (ISettingsService<JobSettings>)app.ApplicationServices.GetService(typeof(ISettingsService<JobSettings>));
            var s = service.GetSettings();
            // Set job factory
            OmbiQuartz.Instance.UseJobFactory(jobFactory);

            // Run configuration
            await AddPlex(s);
            await AddEmby(s);
            await AddDvrApps(s);
            await AddSystem(s);

            // Run Quartz
            await OmbiQuartz.Start();
        }

        private static async Task AddSystem(JobSettings s)
        {
            await OmbiQuartz.Instance.AddJob<IRefreshMetadata>(nameof(IRefreshMetadata), "System", null);
            await OmbiQuartz.Instance.AddJob<IIssuesPurge>(nameof(IIssuesPurge), "System", JobSettingsHelper.IssuePurge(s));
            //OmbiQuartz.Instance.AddJob<IOmbiAutomaticUpdater>(nameof(IOmbiAutomaticUpdater), "System", JobSettingsHelper.Updater(s));
            await OmbiQuartz.Instance.AddJob<INewsletterJob>(nameof(INewsletterJob), "System", JobSettingsHelper.Newsletter(s));
            await OmbiQuartz.Instance.AddJob<IResendFailedRequests>(nameof(IResendFailedRequests), "System", JobSettingsHelper.ResendFailedRequests(s));
            await OmbiQuartz.Instance.AddJob<IMediaDatabaseRefresh>(nameof(IMediaDatabaseRefresh), "System", JobSettingsHelper.MediaDatabaseRefresh(s));
        }

        private static async Task AddDvrApps(JobSettings s)
        {
            await OmbiQuartz.Instance.AddJob<ISonarrSync>(nameof(ISonarrSync), "DVR", JobSettingsHelper.Sonarr(s));
            await OmbiQuartz.Instance.AddJob<IRadarrSync>(nameof(IRadarrSync), "DVR", JobSettingsHelper.Radarr(s));
            await OmbiQuartz.Instance.AddJob<ICouchPotatoSync>(nameof(ICouchPotatoSync), "DVR", JobSettingsHelper.CouchPotato(s));
            await OmbiQuartz.Instance.AddJob<ISickRageSync>(nameof(ISickRageSync), "DVR", JobSettingsHelper.SickRageSync(s));
            await OmbiQuartz.Instance.AddJob<ILidarrArtistSync>(nameof(ILidarrArtistSync), "DVR", JobSettingsHelper.LidarrArtistSync(s));
            await OmbiQuartz.Instance.AddJob<ILidarrAlbumSync>(nameof(ILidarrAlbumSync), "DVR", null);
            await OmbiQuartz.Instance.AddJob<ILidarrAvailabilityChecker>(nameof(ILidarrAvailabilityChecker), "DVR", null);
        }

        private static async Task AddPlex(JobSettings s)
        {
            await OmbiQuartz.Instance.AddJob<IPlexContentSync>(nameof(IPlexContentSync), "Plex", JobSettingsHelper.PlexContent(s), new Dictionary<string, string> { { "recentlyAddedSearch", "false" } });
            await OmbiQuartz.Instance.AddJob<IPlexContentSync>(nameof(IPlexContentSync) + "RecentlyAdded", "Plex", JobSettingsHelper.PlexRecentlyAdded(s), new Dictionary<string, string> { { JobDataKeys.RecentlyAddedSearch, "true" } });
            await OmbiQuartz.Instance.AddJob<IPlexUserImporter>(nameof(IPlexUserImporter), "Plex", JobSettingsHelper.UserImporter(s));
            await OmbiQuartz.Instance.AddJob<IPlexEpisodeSync>(nameof(IPlexEpisodeSync), "Plex", null);
            await OmbiQuartz.Instance.AddJob<IPlexAvailabilityChecker>(nameof(IPlexAvailabilityChecker), "Plex", null);
        }

        private static async Task AddEmby(JobSettings s)
        {
            await OmbiQuartz.Instance.AddJob<IEmbyContentSync>(nameof(IEmbyContentSync), "Emby", JobSettingsHelper.EmbyContent(s));
            await OmbiQuartz.Instance.AddJob<IEmbyEpisodeSync>(nameof(IEmbyEpisodeSync), "Emby", null);
            await OmbiQuartz.Instance.AddJob<IEmbyAvaliabilityChecker>(nameof(IEmbyAvaliabilityChecker), "Emby", null);
            await OmbiQuartz.Instance.AddJob<IEmbyUserImporter>(nameof(IEmbyUserImporter), "Emby", JobSettingsHelper.UserImporter(s));
        }
    }
}