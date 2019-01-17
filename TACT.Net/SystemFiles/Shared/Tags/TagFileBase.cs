﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TACT.Net.Common;
using TACT.Net.SystemFiles.Shared;

namespace TACT.Net.Shared.Tags
{
    public class TagFileBase : SystemFileBase
    {
        public IEnumerable<TagEntry> Tags => _TagEntries.Values;

        protected readonly Dictionary<string, TagEntry> _TagEntries;

        #region Constructors

        protected TagFileBase(TACT container = null) : base(container)
        {
            _TagEntries = new Dictionary<string, TagEntry>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region IO 

        protected void ReadTags(BinaryReader br, uint tagCount, uint entryCount)
        {
            for (int i = 0; i < tagCount; i++)
            {
                var tagEntry = new TagEntry();
                tagEntry.Read(br, entryCount);
                _TagEntries.Add(tagEntry.Name, tagEntry);
            }
        }

        protected void WriteTags(BinaryWriter bw)
        {
            foreach (var tagEntry in SortTags(_TagEntries.Values))
                tagEntry.Write(bw);
        }

        #endregion

        #region Methods

        protected void AddOrUpdateTag(TagEntry tagEntry, int fileCount)
        {
            // initialise the mask for new entries
            if (!_TagEntries.ContainsKey(tagEntry.Name))
                tagEntry.FileMask = new BoolArray((fileCount + 7) / 8);

            _TagEntries[tagEntry.Name] = tagEntry;
        }

        /// <summary>
        /// Removes the specified TagEntry from the collection
        /// </summary>
        /// <param name="tagEntry"></param>
        public void Remove(TagEntry tagEntry) => _TagEntries.Remove(tagEntry.Name);

        protected void RemoveFile(int index)
        {
            if (index > -1)
            {
                foreach (var tagEntry in _TagEntries.Values)
                    tagEntry.FileMask.Remove(index);
            }
        }

        /// <summary>
        /// Returns a TagEntry by name
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="tagEntry"></param>
        /// <returns></returns>
        public bool TryGet(string tag, out TagEntry tagEntry) => _TagEntries.TryGetValue(tag, out tagEntry);

        /// <summary>
        /// Determines if the specific Tag exists
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool ContainsTag(string tag) => _TagEntries.ContainsKey(tag);

        protected IEnumerable<string> GetTags(int index)
        {
            if (index > -1)
            {
                foreach (var tagEntry in _TagEntries.Values)
                    if (tagEntry.FileMask[index])
                        yield return tagEntry.Name;
            }
        }


        protected void SetTags(int index, params string[] tags)
        {
            if (index > -1)
            {
                if (tags == null)
                    tags = _TagEntries.Keys.ToArray();

                var _ = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var tagEntry in _TagEntries.Values)
                    tagEntry.FileMask[index] = _.Contains(tagEntry.Name);
            }
        }

        protected void SetTags(int index, bool value, params string[] tags)
        {
            if (index > -1)
            {
                foreach (var tag in tags)
                    if (_TagEntries.TryGetValue(tag, out var tagEntry))
                        tagEntry.FileMask[index] = value;
            }
        }

        #endregion

        #region Helpers

        private IOrderedEnumerable<TagEntry> SortTags(IEnumerable<TagEntry> tagEntries)
        {
            // order by type then name, Alternate is Locale although differentiated
            return tagEntries.OrderBy(x => x.TypeId == 0x4000 ? 3 : x.TypeId).ThenBy(x => x.Name);
        }

        #endregion
    }
}
