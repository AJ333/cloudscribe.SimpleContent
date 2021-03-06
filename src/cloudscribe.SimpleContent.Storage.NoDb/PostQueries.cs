﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-04-24
// Last Modified:           2016-09-23
// 

using cloudscribe.SimpleContent.Models;
using Microsoft.Extensions.Logging;
using NoDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Storage.NoDb
{
    public class PostQueries : IPostQueries
    {
        public PostQueries(
            IBasicCommands<Post> postCommands,
            IBasicQueries<Post> postQueries,
            ILogger<PostQueries> logger)
        {
            commands = postCommands;
            query = postQueries;
            log = logger;
        }

        private IBasicCommands<Post> commands;
        private IBasicQueries<Post> query;
        private ILogger log;
        
        
        public async Task<List<Post>> GetAllPosts(
            string blogId,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            //TODO: caching
            //if (HttpRuntime.Cache["posts"] == null)

            var l = await query.GetAllAsync(blogId, cancellationToken).ConfigureAwait(false);
            var list = l.ToList();
            
            //if (list.Count > 0)
            //{
            //    list.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
            //    //HttpRuntime.Cache.Insert("posts", list);
            //}

            return list;

            //if (HttpRuntime.Cache["posts"] != null)
            //{
            //    return (List<Post>)HttpRuntime.Cache["posts"];
            //}
            //return new List<Post>();
        }

        public async Task<List<IPost>> GetPosts(
            string blogId,
            bool includeUnpublished,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var list = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);

            list = list.Where(p =>
                      (includeUnpublished || (p.IsPublished && p.PubDate <= DateTime.UtcNow))
                      ).ToList<Post>();

            if (list.Count > 0)
            {
                list.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
                //HttpRuntime.Cache.Insert("posts", list);
            }

            var result = new List<IPost>();
            result.AddRange(list);

            return result;
        }

        public async Task<PagedPostResult> GetPosts(
            string blogId,
            string category,
            bool includeUnpublished,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            //var posts = await GetPosts(blogId, includeUnpublished, cancellationToken);
            var posts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);


            var totalPosts = posts.Count;

            if (!string.IsNullOrEmpty(category))
            {
                //var i = posts as IEnumerable<Post>;

                posts = posts.Where(p =>
                    (includeUnpublished || (p.IsPublished && p.PubDate <= DateTime.UtcNow))
                     && p.Categories.Any(
                        c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
                        )
                        .OrderByDescending(p => p.PubDate)
                        .ToList<Post>();

                totalPosts = posts.Count;

            }
            else
            {
                posts = posts.OrderByDescending(p => p.PubDate).ToList<Post>();
            }

            if (pageSize > 0)
            {

                var offset = 0;
                if (pageNumber > 1) { offset = pageSize * (pageNumber - 1); }
                posts = posts.Skip(offset).Take(pageSize).ToList<Post>();


            }

            var data = new List<IPost>();
            data.AddRange(posts);

            var result = new PagedPostResult();
            result.Data = data;
            result.TotalItems = totalPosts;

            return result;
        }

        public async Task<int> GetCount(
            string blogId,
            string category,
            bool includeUnpublished,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            //var posts = await GetPosts(blogId, includeUnpublished, cancellationToken);
            var list = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);

            list = list.Where(p =>
                      (includeUnpublished || (p.IsPublished && p.PubDate <= DateTime.UtcNow))
                      ).ToList<Post>();

            if (!string.IsNullOrEmpty(category))
            {
                list = list.Where(
                    p => p.Categories.Any(
                        c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
                        ).ToList<Post>();
            }
            
            return list.Count();
        }

        public async Task<List<IPost>> GetRecentPosts(
            string blogId,
            int numberToGet,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var posts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);

            posts = posts.Where(p =>
                p.IsPublished
                && p.PubDate <= DateTime.UtcNow)
                .OrderByDescending(p => p.PubDate)
                .Take(numberToGet).ToList<Post>();

            var result = new List<IPost>();
            result.AddRange(posts);

            return result;

        }

        public async Task<PagedPostResult> GetPosts(
            string blogId,
            int year,
            int month = 0,
            int day = 0,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var posts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);
            var totalItems = posts.Count;

            if (day > 0 && month > 0)
            {
                posts = posts.Where(
                x => x.PubDate.Year == year
                && x.PubDate.Month == month
                && x.PubDate.Day == day
                && (includeUnpublished || (x.IsPublished
                && x.PubDate <= DateTime.UtcNow))
                )
                .ToList<Post>();
            }
            else if (month > 0)
            {
                posts = posts.Where(
                x => x.PubDate.Year == year
                && x.PubDate.Month == month
                && (includeUnpublished || (x.IsPublished
                && x.PubDate <= DateTime.UtcNow))
                )
                .ToList<Post>();

            }
            else
            {
                posts = posts.Where(
                x => x.PubDate.Year == year
                )
                .ToList<Post>();
            }

            if (pageSize > 0)
            {
                var offset = 0;
                if (pageNumber > 1) { offset = pageSize * (pageNumber - 1); }
                posts = posts.Skip(offset).Take(pageSize).ToList<Post>();

            }

            var result = new PagedPostResult();

            var data = new List<IPost>();
            data.AddRange(posts);

            result.Data = data;
            result.TotalItems = totalItems;

            return result;

            //return posts.Where(
            //    x => x.PubDate.Year == year
            //    )
            //    .Take(pageSize).ToList<Post>();

        }

        public async Task<int> GetCount(
            string blogId,
            int year,
            int month = 0,
            int day = 0,
            bool includeUnpublished = false,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var posts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);

            if (day > 0 && month > 0)
            {
                return posts.Where(
                x => x.PubDate.Year == year
                && x.PubDate.Month == month
                && x.PubDate.Day == day
                && (includeUnpublished || (x.IsPublished
                && x.PubDate <= DateTime.UtcNow))
                )
                .Count();
            }
            else if (month > 0)
            {
                return posts.Where(
                x => x.PubDate.Year == year
                && x.PubDate.Month == month
                && (includeUnpublished || (x.IsPublished
                && x.PubDate <= DateTime.UtcNow))
                )
                .Count();

            }

            return posts.Where(
                x => x.PubDate.Year == year
                )
                .Count();

        }

        public async Task<IPost> GetPost(
            string blogId,
            string postId,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            return await query.FetchAsync(blogId, postId, cancellationToken).ConfigureAwait(false);
            //var allPosts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);
            //return allPosts.FirstOrDefault(p => p.Id == postId);

        }

        public async Task<PostResult> GetPostBySlug(
            string blogId,
            string slug,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var result = new PostResult();
            var allPosts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);
            result.Post = allPosts.FirstOrDefault(p => p.Slug == slug);

            if(result.Post != null)
            {
                var cutoff = result.Post.PubDate;

                result.PreviousPost = allPosts
                    .Where(
                    p => p.PubDate < cutoff
                    && p.IsPublished == true
                    )
                    .OrderByDescending(p => p.PubDate)
                    .Take(1)
                    .FirstOrDefault();
                
                result.NextPost = allPosts
                    .Where(
                    p => p.PubDate > cutoff
                    && p.IsPublished == true
                    )
                    .OrderBy(p => p.PubDate)
                    .Take(1)
                    .FirstOrDefault();

            }

            return result;
        }

        public async Task<bool> SlugIsAvailable(
            string blogId,
            string slug,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var allPosts = await GetAllPosts(blogId, cancellationToken).ConfigureAwait(false);

            var isInUse = allPosts.Any(
                p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

            return !isInUse;
        }

        public async Task<Dictionary<string, int>> GetCategories(
            string blogId,
            bool includeUnpublished,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var result = new Dictionary<string, int>();

            var visiblePosts = await GetPosts(
                blogId,
                includeUnpublished,
                cancellationToken).ConfigureAwait(false);

            foreach (var category in visiblePosts.SelectMany(post => post.Categories))
            {
                var c = category.Trim().ToLowerInvariant();
                if (!result.ContainsKey(c))
                {
                    result.Add(c, 0);
                }

                result[c] = result[c] + 1;
            }

            // TODO: cache this 
            var sorted = new SortedDictionary<string, int>(result);

            return sorted.OrderBy(x => x.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) as Dictionary<string, int>;
        }

        public async Task<Dictionary<string, int>> GetArchives(
            string blogId,
            bool includeUnpublished,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var result = new Dictionary<string, int>();

            // since we are storing the files on disk grouped by year and month folders
            // we could derive this list from the file system
            // currently we are loading all posts into memory but for a blog with years of frequent posts
            // maybe we don't need to keep all the old posts in memory we could load the recent year(s) by default and
            // load the old years on demand if requests come in for them
            // at any rate I think the way of retrieving posts needs more review and thought
            // about efficient strategies to use the minimum resources needed

            var visiblePosts = await GetPosts(
                blogId,
                includeUnpublished,
                cancellationToken).ConfigureAwait(false);

            var grouped = from p in visiblePosts
                          group p by new { month = p.PubDate.Month, year = p.PubDate.Year } into d
                          select new
                          {
                              key = d.Key.year.ToString() + "/" + d.Key.month.ToString("00")
                              ,
                              count = d.Count()
                          };

            foreach (var item in grouped)
            {
                result.Add(item.key, item.count);
            }

            return result;

            //foreach (var category in visiblePosts.SelectMany(post => post.Categories))
            //{
            //    if (!result.ContainsKey(category))
            //    {
            //        result.Add(category, 0);
            //    }

            //    result[category] = result[category] + 1;
            //}

            // TODO: cache this 
            //var sorted = new SortedDictionary<string, int>(result);

            //return sorted.OrderBy(x => x.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) as Dictionary<string, int>;
        }

    }
}
