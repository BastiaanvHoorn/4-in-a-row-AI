using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace Server
{
    public class SegmentReader
    {
        DatabaseSegment DbSegment;
        int Position;
        byte[] CompField;

        public SegmentReader(DatabaseSegment dbSeg)
        {
            this.DbSegment = dbSeg;
            this.Position = -1;
        }

        public byte[] getCurrentCompField()
        {
            return CompField;
        }

        public FieldData getCurrentFieldData()
        {
            return DbSegment.readFieldData(Position);
        }

        public bool nextPosition()
        {
            Position++;

            if (endOfSegment())
                return false;

            CompField = DbSegment.readCompressedField(Position);

            return true;
        }

        public bool endOfSegment()
        {
            return Position == DbSegment.FieldCount;
        }
    }

    public static class SegmentMerger
    {
        public static void merge(DatabaseSegment destSegment, params DatabaseSegment[] segments)
        {
            int fieldLength = segments[0].FieldLength;
            List<SegmentReader> segReaders = new List<SegmentReader>(segments.Length);

            foreach (DatabaseSegment seg in segments)
            {
                SegmentReader reader = new SegmentReader(seg);
                if (reader.nextPosition())
                    segReaders.Add(reader);
            }

            while (segReaders.Any())
            {
                byte[] smallestValue = segReaders[0].getCurrentCompField();

                foreach (SegmentReader r in segReaders)
                {
                    CompressedComparison comparison = compareCompFields(r.getCurrentCompField(), smallestValue);

                    if (comparison == CompressedComparison.Smaller)
                    {
                        smallestValue = r.getCurrentCompField();
                    }
                }

                FieldData fieldData = new FieldData();

                bool readerDone = false;

                foreach (SegmentReader r in segReaders)
                {
                    if (compareCompFields(smallestValue, r.getCurrentCompField()) == CompressedComparison.Equal)
                    {
                        fieldData += r.getCurrentFieldData();

                        if (!r.nextPosition())
                            readerDone = true;
                    }
                }

                if (readerDone)
                    segReaders.RemoveAll(r => r.endOfSegment());

                Field field = smallestValue.decompressField();

                destSegment.appendItem(field, fieldData);
            }

            destSegment.writeProperties();
        }

        private static CompressedComparison compareCompFields(byte[] fComp1, byte[] fComp2)
        {
            for (int i = 0; i < fComp1.Length; i++)
            {
                if (fComp1[i] < fComp2[i])
                {
                    return CompressedComparison.Smaller;
                }
                else if (fComp1[i] > fComp2[i])
                {
                    return CompressedComparison.Greater;
                }
            }

            return CompressedComparison.Equal;
        }

        private enum CompressedComparison
        {
            Equal,
            Smaller,
            Greater
        }
    }
}
