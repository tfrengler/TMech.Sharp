using System;
using System.Collections.Generic;
using System.Linq;

namespace TMech.Sharp.Browsers
{
    internal readonly struct Version : IComparer<Version>, IComparable<Version>
    {
        public int Major { get; init; }
        public int Minor { get; init; }

        public int Compare(Version x, Version y)
        {
            if (x.Major > y.Major || (x.Major == y.Major && x.Minor > y.Minor)) return 1;
            if (x.Major < y.Major || (x.Major == y.Major && x.Minor < y.Minor)) return -1;

            return 0;
        }

        public int CompareTo(Version other)
        {
            return Compare(this, other);
        }

        public static Version FromString(string input)
        {
            if (input.Length == 0) return new Version();

            input = input.TrimStart('v'); // Because of Geckodriver whose version string has a leading 'v'
            string[] VersionParts = input.Split('.');
            int MajorRev = Convert.ToInt32(VersionParts[0]);

            if (VersionParts.Length == 1) return new Version() { Major = MajorRev };

            int MinorRev = VersionParts.Skip(1).Sum(Convert.ToInt32);
            return new Version() { Major = MajorRev, Minor = MinorRev };
        }
    }
}
