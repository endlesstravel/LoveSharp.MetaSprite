using System.Collections.Generic;
using System.Linq;

namespace MetaSprite
{
    public class AnimationClip
    {
        public readonly bool IsReversed;
        public readonly bool loopTime;
        public readonly string Name;

        public IReadOnlyList<Sprite> Frames => spriteFrameList;
        List<Sprite> spriteFrameList = new List<Sprite>();
        public IReadOnlyList<float> ElapsedTimeList => elapsedTimeList;
        public IReadOnlyList<float> ElapsedStartTimeList => elapsedStartTimeList;
        public float Duration => duration;
        List<float> elapsedTimeList = new List<float>();
        List<float> elapsedStartTimeList = new List<float>();
        float duration;

        #region frame info
        public AnimationClip(FrameTag tag, List<Sprite> sprites, List<Frame> frames, bool reverseFrameList)
        {
            Name = tag.name;
            IsReversed = reverseFrameList;

            // Set loop property
            loopTime = tag.properties.Contains("loop");

            var normalFrameList = new List<Sprite>((tag.to - tag.from + 1) * 2);
            for (int i = tag.from; i <= tag.to; ++i) normalFrameList.Add(sprites[i]);

            if (reverseFrameList)
            {
                normalFrameList.Reverse();
            }

            // set frame
            if (tag.dir == AnimationDirection.Forward) // ex: 1, 2, 3, 4
            {
                spriteFrameList = normalFrameList; // do noting ...
            }
            if (tag.dir == AnimationDirection.Reverse)  // ex: 4, 3, 2, 1
            {
                normalFrameList.Reverse();
                spriteFrameList = normalFrameList;

            }
            else if (tag.dir == AnimationDirection.PingPong) // ex: 1, 2, 3, 4, 3, 2
            {
                spriteFrameList = new List<Sprite>(normalFrameList);
                spriteFrameList.AddRange(normalFrameList.Skip(1).Take(normalFrameList.Count - 2).Reverse());
            }

            elapsedTimeList = new List<float>(spriteFrameList.Count);
            elapsedStartTimeList = new List<float>(spriteFrameList.Count);
            float elapsedTime = 0;
            foreach (var item in spriteFrameList)
            {
                elapsedStartTimeList.Add(elapsedTime);
                elapsedTime += item.duration;
                elapsedTimeList.Add(elapsedTime);
            }

            duration = elapsedTime;
        }
        #endregion


        public int FindFrame(float tt)
        {
            // 二分法进行查找
            var list = ElapsedTimeList;
            var g = tt % list[list.Count - 1];
            int max = list.Count - 1;

            // 二分法查找，比 timeElased 小的 index
            int l = 0;
            int r = max;
            int m = (l + r) / 2;

            while (l < r)
            {
                var v = list[m];
                if (g > v)
                {
                    l = m;
                }
                else
                {
                    r = m;
                }

                m = (l + r) / 2;
            }

            return m;
        }

        #region subimage
        public bool isSubImage => SubImageTagName != null;
        public string SubImageTagName;
        readonly Dictionary<string, AnimationClip> SubImage = new Dictionary<string, AnimationClip>();
        #endregion



        #region draw region
        #endregion
    }
}
