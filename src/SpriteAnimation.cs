using System.Linq;
using System.Collections.Generic;
using Love;
using System;

namespace MetaSprite
{
    public class SpriteAnimation
    {
        #region 
        public static SpriteAnimation New(string path, string initTag)
        {
            return ASEImporter.Import(path, initTag);
        }
        #endregion


        AnimationClip currentTag = null;
        Sprite currentFrame => FrameCount > 0 ? currentTag?.Frames[CurrentFrameIndex] : null;
        public int FrameCount => currentTag?.Frames.Count ?? 0;
        public string TagName => currentTag?.Name;
        public bool IsPaused { private set; get; }

        Dictionary<string, AnimationClip> renderableFrameTagDict;
        Dictionary<string, AnimationClip> reversedRenderableFrameTagDict;

        public readonly int Width, Height;

        public bool TryGetCurrentFrameRect(string key, out RectangleF r)
        {
            return CurrentFrameRectDict.TryGetValue(key, out r);
        }

        public bool TryGetCurrentFrameTrans(string key, out Vector2 p)
        {
            return CurrentFrameTransDict.TryGetValue(key, out p);
        }

        public IEnumerable<string> CurrentFrameRectKeys => currentFrame.rectDict.Keys;
        public Dictionary<string, RectangleF> CurrentFrameRectDict
        {
            get
            {
                if (currentFrame == null)
                    return null;

                var pof = CurrentFramePiovtOffset;

                var dict = new Dictionary<string, RectangleF>();
                foreach (var kv in currentFrame.rectDict)
                {
                    dict[kv.Key] = new RectangleF(
                        kv.Value.X - (pof.X * Width), 
                        kv.Value.Y - (pof.Y * Height),
                        kv.Value.Width, kv.Value.Height);
                }
                return dict;
            }
        }

        public IEnumerable<string> CurrentFrameTransKeys => currentFrame.transDict.Keys;
        public Dictionary<string, Vector2> CurrentFrameTransDict
        {
            get
            {
                if (currentFrame == null)
                    return null;

                var pof = CurrentFramePiovtOffset;
                var dict = new Dictionary<string, Vector2>();
                foreach (var kv in currentFrame.transDict)
                {
                    dict[kv.Key] = new Vector2(kv.Value.X - (pof.X * Width), kv.Value.Y - (pof.Y * Height));
                }
                return dict;
            }
        }
        //public Vector2 CurrentFrameTransToPos(Vector2 pos, Vector2 trans)
        //{
        //    var pof = CurrentFramePiovtOffset;
        //    return new Vector2(pos.X + trans.X - (pof.X * Width), pos.Y + trans.Y - (pof.Y * Height));
        //}
        //public RectangleF CurrentFrameRectToPos(Vector2 pos, RectangleF rect)
        //{
        //    var pof = CurrentFramePiovtOffset;
        //    return new RectangleF(
        //                pos.X + rect.X - (pof.X * Width), pos.Y + rect.Y - (pof.Y * Height),
        //                rect.Width, rect.Height);
        //}
        public Vector2 CurrentFramePiovtOffset => currentFrame.spritedPivot;

        /// <summary>
        /// all tag name
        /// </summary>
        public IEnumerable<string> TagNameIter => renderableFrameTagDict.Keys;

        public SpriteAnimation(Dictionary<string, AnimationClip> dict, Dictionary<string, AnimationClip> reversedAniDict, int widht, int height, string initialTag)
        {
            this.renderableFrameTagDict = dict ?? throw new System.ArgumentNullException(nameof(dict));
            this.reversedRenderableFrameTagDict = reversedAniDict ?? throw new System.ArgumentNullException(nameof(dict));
            ReallySetTag(initialTag, false);
            IsPaused = false;

            Width = widht;
            Height = height;
        }



        /// <summary>
        /// same as SetTagimmediately, but this function will wait unit Update called to adjust to change.
        /// </summary>
        public void SetTag(string tagName, bool reverseMode = false)
        {
            if (tagName == null) throw new Exception("No animation tag specified!");
            if (renderableFrameTagDict.ContainsKey(tagName) == false) throw new Exception($"Tag {tagName} not found in frametags!");
            wantToChangedTag = tagName;
            wantToChangedTagReverseMode = reverseMode;
        }

        /// <summary>
        /// Switch to a different animation tag.
        /// In the case that we're attempting to switch to the animation currently playing,
        /// nothing will happen.
        /// </summary>
        public void SetTagimmediately(string tagName)
        {
            wantToChangedTag = null;
            ReallySetTag(tagName, wantToChangedTagReverseMode);
        }

        string wantToChangedTag = null;
        bool wantToChangedTagReverseMode = false;

        void ReallySetTag(string tagName, bool reverseMode)
        {
            var rdict = reverseMode ? reversedRenderableFrameTagDict : renderableFrameTagDict;
            if (tagName == null) throw new Exception("No animation tag specified!");
            if (rdict.ContainsKey(tagName) == false) throw new Exception($"Tag {tagName} not found in frametags!");

            if (currentTag!= null && currentTag.Name == tagName && currentTag.IsReversed == reverseMode) // same ... then return;
                return;

            currentTag = rdict[tagName];

            if (currentTag.Frames.Count <= 0)
            {
                //throw new Exception($"Tag {tagName} not found in frametags!");
                throw new Exception($"invalid tag {tagName} has negative frame count: {currentTag.Frames.Count}");
            }

            // set it manually
            CurrentFrameIndex = 0;
            TimeElapsed = 0;
            isNeedStartToCallFrameAction = true;
        }

        /// <summary>
        /// Jump to a particular frame index in the current animation.
        /// Errors if the frame is outside the tag's frame range.
        /// </summary>
        /// <param name="frame"></param>
        public void SetFrame(int frame)
        {
            if (frame < 0 || frame >= FrameCount)
            {
                throw new ArgumentOutOfRangeException($"Frame {frame} is out of range of tag '{currentTag.Name}' [0..{currentTag.Frames.Count})");
            }

            CurrentFrameIndex = frame;
            TimeElapsed = currentTag.ElapsedStartTimeList[CurrentFrameIndex];
            isNeedStartToCallFrameAction = true;
        }

        /// <summary>
        /// Draw the animation's current frame in a specified location.
        /// </summary>
        public void Draw(float x, float y, float rot = 0, float sx = 1, float sy = 1, float ox = 0, float oy = 0)
        {
            if (currentFrame != null)
            {
                Graphics.Draw(currentFrame.quad, currentFrame.image, x, y, rot, sx, sy, 
                    (currentFrame.pivot.X) * currentFrame.rect.Width + (Width * currentFrame.spritedPivot.X) + ox,
                    (1 - currentFrame.pivot.Y) * currentFrame.rect.Height - Height + (Height * currentFrame.spritedPivot.Y) + oy);
            }
        }


        public int CurrentFrameIndex
        {
            get;
            private set;
        }

        public float TimeElapsed
        {
            get;
            private set;
        }

        bool isNeedStartToCallFrameAction = false;

        /// <summary>
        /// Update the animation.
        /// </summary>
        public void Update(float dt)
        {
            if (wantToChangedTag != null)
            {
                ReallySetTag(wantToChangedTag, wantToChangedTagReverseMode);
                wantToChangedTag = null;
                wantToChangedTagReverseMode = false;
            }

            if (IsPaused)
                return;

            if (dt == 0)
                return;

            if (dt < 0)
                throw new Exception($"{nameof(dt)} must be positive");

            if (currentTag == null)
                throw new Exception("not set tag yet");

            if (currentTag.loopTime)
            {
                var lastFrame = CurrentFrameIndex;
                var list = ElapsedTimeMoveFrame(CurrentFrameIndex, TimeElapsed, dt);

                if (isNeedStartToCallFrameAction)
                {
                    FrameBegin?.Invoke(currentTag.Name, 0);
                    isNeedStartToCallFrameAction = false;
                }
                foreach (var itemIndex in list)
                {
                    FrameBegin?.Invoke(currentTag.Name, itemIndex);
                }

                // add the remain ....
                CurrentFrameIndex = list?.Last?.Value ?? CurrentFrameIndex;
                TimeElapsed += dt;
            }
            else
            {
                var lastFrameIndex = currentTag.Frames.Count - 1;
                if (CurrentFrameIndex == lastFrameIndex)
                {
                    // do nothing ...

                }
                else
                {
                    var curFrame = CurrentFrameIndex;
                    var list = ElapsedTimeMoveFrame(CurrentFrameIndex, TimeElapsed, dt);

                    // invoke ....
                    if (isNeedStartToCallFrameAction)
                    {
                        FrameBegin?.Invoke(currentTag.Name, 0);
                        isNeedStartToCallFrameAction = false;
                    }
                    foreach (var itemIndex in list)
                    {
                        if (itemIndex <= CurrentFrameIndex) // looped check
                            break;

                        curFrame = itemIndex;
                        FrameBegin?.Invoke(currentTag.Name, itemIndex);
                        if (itemIndex == lastFrameIndex) // end of frame
                        {
                            break;
                        }
                    }

                    // add the remain ....
                    CurrentFrameIndex = curFrame;
                    TimeElapsed += dt;
                }

            }
        }

        public event Action<string, int> FrameBegin;


        LinkedList<int> ElapsedTimeMoveFrame(int startFrame, float startTime, float dt, bool falg = true)
        {
            //var odd = endTime % currentTag.Duration;
            //var total = endTime - odd;
            //List<RenderableFrame> list = new List<RenderableFrame>();
            //list.AddRange();
            //Mathf.RoundToInt();

            if (falg)
            {
                var count = ElapsedTimeMoveFrame(startFrame, startTime, dt, false);
                if (count.Count > 5)
                {
                    Console.Title = "";
                }
            }

            // TODO:
            // 可以通过取余优化
            var list = new LinkedList<int>();
            var elapsedList = currentTag.ElapsedTimeList;
            var frameList = currentTag.Frames;
            var espt = elapsedList[startFrame] - (startTime % currentTag.Duration);
            if (espt > 0)
                dt -= espt; // 减去最初的一个 frame 时间
            int index = (startFrame + 1) % elapsedList.Count;

            while (dt > 0)
            {
                list.AddLast(index);

                index = (index + 1) % elapsedList.Count;
                espt = frameList[index].duration;
                if (espt > 0)
                    dt -= espt;
                else
                    dt -= 0.001f; // for error
            }

            return list;
        }


        public void Pause() => IsPaused = true;
        public void Play() => IsPaused = false;

        /// <summary>
        /// Stops the animation (pause it then return to first frame or last if specified)
        /// </summary>
        public void Stop(bool onLast = false)
        {
            IsPaused = true;
            SetFrame(onLast ? currentTag.Frames.Count - 1 : 0);
        }
    }
}