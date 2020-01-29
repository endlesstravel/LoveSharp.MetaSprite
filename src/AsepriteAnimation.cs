using System.Linq;
using System.Collections.Generic;
using Love;
using System;

namespace MetaSprite
{
    public class AsepriteAnimation
    {
        #region 
        public static AsepriteAnimation New(string path, string initTag)
        {
            return ASEImporter.Import(path, initTag);
        }

        /// <summary>
        /// clone of this
        /// </summary>
        public AsepriteAnimation Clone()
        {
            return new AsepriteAnimation(this);
        }

        /// <summary>
        /// clone of other
        /// </summary>
        public AsepriteAnimation(AsepriteAnimation other)
        {
            renderableFrameTagDict = other.renderableFrameTagDict;
            reversedRenderableFrameTagDict = other.reversedRenderableFrameTagDict;
            ReallySetTag(this.TagNameIter.FirstOrDefault(), false);
            IsPaused = false;

            Width = other.Width;
            Height = other.Height;
        }
        public AsepriteAnimation(
            Dictionary<string, AnimationClip> dict, Dictionary<string, AnimationClip> reversedAniDict,
            int widht, int height, string initialTag)
        {
            this.renderableFrameTagDict = dict ?? throw new System.ArgumentNullException(nameof(dict));
            this.reversedRenderableFrameTagDict = reversedAniDict ?? throw new System.ArgumentNullException(nameof(dict));

            ReallySetTag(initialTag, false);
            IsPaused = false;

            Width = widht;
            Height = height;
        }

        #endregion

        public bool IsReverseMode { get; private set; }

        AnimationClip currentTag = null;
        Sprite currentFrame => FrameCount > 0 ? currentTag?.Frames[CurrentFrameIndex] : null;
        public int FrameCount => currentTag?.Frames.Count ?? 0;
        public string TagName => currentTag?.Name;
        public int RenderingImageAtlasWidth => currentFrame.image.GetWidth();
        public int RenderingImageAtlasHeight => currentFrame.image.GetHeight();
        public float CurrentFrameDuration => currentTag.Duration;
        public bool IsPaused { private set; get; }

        readonly Dictionary<string, AnimationClip> renderableFrameTagDict;
        readonly Dictionary<string, AnimationClip> reversedRenderableFrameTagDict;

        public readonly int Width, Height;

        public bool TryGetDuration(string key, out float duration)
        {
            duration = 0;
            if (renderableFrameTagDict.TryGetValue(key, out var acp))
            {
                duration = acp.Duration;
                return true;
            }

            return false;
        }

        public bool TryGetCurrentFrameRect(string key, out RectangleF r)
        {
            return CurrentFrameRectDict.TryGetValue(key, out r);
        }

        public bool TryGetCurrentFrameTrans(string key, out Vector2 p)
        {
            return CurrentFrameTransDict.TryGetValue(key, out p);
        }

        public IReadOnlyCollection<string> GetFrameEvent(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= FrameCount)
            {
                throw new ArgumentOutOfRangeException($"Frame {frameIndex} is out of range of tag '{currentTag.Name}' [0..{currentTag.Frames.Count})");
            }

            return currentTag.Frames[frameIndex].eventSet;
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
                        kv.Value.X - (pof.X), 
                        kv.Value.Y - (pof.Y),
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
                    dict[kv.Key] = new Vector2(kv.Value.X - (pof.X ), kv.Value.Y - (pof.Y));
                }
                return dict;
            }
        }

        public Vector2 CurrentFramePiovtOffset => currentFrame.spritedPivot;

        /// <summary>
        /// all tag name
        /// </summary>
        public IEnumerable<string> TagNameIter => renderableFrameTagDict.Keys;



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

        public bool GetLoop(string tagName)
        {
            if (tagName == null) throw new Exception("No animation tag specified!");
            if (renderableFrameTagDict.TryGetValue(tagName, out var ac) == false) throw new Exception($"Tag {tagName} not found in frametags!");
            return ac.loopTime;
        }

        public void SetLoop(string tagName, bool loop)
        {
            if (tagName == null) throw new Exception("No animation tag specified!");
            if (renderableFrameTagDict.TryGetValue(tagName, out var ac) == false) throw new Exception($"Tag {tagName} not found in frametags!");
            ac.loopTime = loop;
        }

        /// <summary>
        /// Switch to a different animation tag.
        /// In the case that we're attempting to switch to the animation currently playing,
        /// nothing will happen.
        /// </summary>
        public void SetTagImmediately(string tagName)
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
            IsReverseMode = reverseMode;

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
        /// !!! This function will wait unit Update called to adjust to change. !!!
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

            wantToSetFrameIndex = frame;
            wantToSetFrameIndexFlag = true;
            isNeedStartToCallFrameAction = true;
        }

        /// <summary>
        /// Jump to a particular frame index in the current animation.
        /// Errors if the frame is outside the tag's frame range.
        /// </summary>
        /// <param name="frame"></param>
        public void SetFrameImmediately(int frame)
        {
            if (frame < 0 || frame >= FrameCount)
            {
                throw new ArgumentOutOfRangeException($"Frame {frame} is out of range of tag '{currentTag.Name}' [0..{currentTag.Frames.Count})");
            }

            wantToSetFrameIndexFlag = false;
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
                    (-currentFrame.imgQuadOffset.X + currentFrame.spritedPivot.X),
                    (-currentFrame.imgQuadOffset.Y + currentFrame.spritedPivot.Y)
                    );
            }
        }

        /// <summary>
        /// Draw the animation's current frame in a specified location.
        /// </summary>
        public void Draw(Vector2 pos, float scale)
        {
            Draw(pos.X,  pos.Y, 0, scale, scale);
        }

        /// <summary>
        /// Draw the animation's current frame in a specified location.
        /// </summary>
        public void Draw(Action<Quad, Image, Vector2> drawFunc)
        {
            if (currentFrame != null)
            {
                drawFunc?.Invoke(currentFrame.quad, currentFrame.image,
                    new Vector2(
                        (-currentFrame.imgQuadOffset.X + currentFrame.spritedPivot.X),
                        (-currentFrame.imgQuadOffset.Y + currentFrame.spritedPivot.Y)
                    ));
            }
        }



        class SpriteAnimationSubarea
        {
            public RectangleF Rect => rect;
            readonly internal RectangleF rect;
            readonly public Quad quad;
            readonly internal Vector2 offset;

            internal SpriteAnimationSubarea(RectangleF rect, Quad quad, Vector2 offset)
            {
                this.rect = rect;
                this.quad = quad;
                this.offset = offset;
            }
        }
        SpriteAnimationSubarea GenSubRegionQuad(RectangleF subArea)
        {
            var vpr = (currentFrame.quad.GetViewport());
            var original_srect = new RectangleF(
                vpr.X + subArea.X - currentFrame.imgQuadOffset.X + currentFrame.spritedPivot.X, 
                vpr.Y + subArea.Y - currentFrame.imgQuadOffset.Y + currentFrame.spritedPivot.Y,
                subArea.Width, subArea.Height);
            var srect = RectangleF.Intersect(vpr, original_srect);
            var sub_quad = Graphics.NewQuad(srect.X, srect.Y, srect.Width, srect.Height, currentFrame.image.GetWidth(), currentFrame.image.GetHeight());
            return new SpriteAnimationSubarea(subArea, sub_quad, new Vector2(srect.X - vpr.X, srect.Y - vpr.Y));
        }


        /// <summary>
        /// Draw the animation's current frame in a specified location.
        /// </summary>
        void DrawSubRegion(SpriteAnimationSubarea subArea, float x, float y, float rot = 0, float sx = 1, float sy = 1, float ox = 0, float oy = 0)
        {
            if (currentFrame != null)
            {
                Graphics.Draw(subArea.quad, currentFrame.image,
                    x,  y, rot, sx, sy,
                    (-currentFrame.imgQuadOffset.X + currentFrame.spritedPivot.X) - subArea.offset.X,
                    (-currentFrame.imgQuadOffset.Y + currentFrame.spritedPivot.Y) - subArea.offset.Y
                    );
            }
        }

        /// <summary>
        /// Draw the animation's current frame in a specified location.
        /// </summary>
        public void DrawSubRegion(RectangleF subAreaRect, float x, float y, float rot = 0, float sx = 1, float sy = 1, float ox = 0, float oy = 0)
        {
            if (currentFrame != null)
            {
                DrawSubRegion(GenSubRegionQuad(subAreaRect), x, y, rot, sx, sy, ox, oy);
            }
        }

        //public RectangleF GetCurrentQuadViewport()
        //{
        //    var vp = currentFrame.quad.GetViewport();
        //    return new RectangleF(vp.x, vp.y, vp.w, vp.h);
        //}


        /// <summary>
        /// Draw the animation's current frame in a specified location. for scale
        /// </summary>
        public void DrawSubRegion(Action<Quad, Image, Vector2, Vector2> drawFunc, RectangleF subAreaRect)
        {
            if (currentFrame != null)
            {
                var subArea = GenSubRegionQuad(subAreaRect);
                drawFunc?.Invoke(subArea.quad, currentFrame.image, new Vector2(-subArea.rect.X, -subArea.rect.Y),
                    new Vector2(
                        (-currentFrame.imgQuadOffset.X + currentFrame.spritedPivot.X) - subArea.offset.X,
                        (-currentFrame.imgQuadOffset.Y + currentFrame.spritedPivot.Y) - subArea.offset.Y
                    ));
            }
        }

        bool wantToSetFrameIndexFlag = false;
        int wantToSetFrameIndex = 0;

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
            if (IsPaused)
                return;

            if (wantToChangedTag != null)
            {
                ReallySetTag(wantToChangedTag, wantToChangedTagReverseMode);
                wantToChangedTag = null;
                wantToChangedTagReverseMode = false;
            }
            
            if (wantToSetFrameIndexFlag)
            {
                SetFrameImmediately(wantToSetFrameIndex);
            }

            if (dt == 0)
                return;

            if (dt < 0)
                throw new Exception($"{nameof(dt)} must be positive");

            if (currentTag == null)
                throw new Exception("not set tag yet");

            string currentExecuteTagName = currentTag.Name;


            // invoke ....
            if (isNeedStartToCallFrameAction)
            {
                FrameBegin?.Invoke(currentExecuteTagName, CurrentFrameIndex);
                isNeedStartToCallFrameAction = false;
            }

            if (currentTag.loopTime)
            {
                var lastFrame = CurrentFrameIndex;
                ElapsedTimeMoveFrame_LoopMode(CurrentFrameIndex, TimeElapsed, dt,
                    out var beginFrameList, out var endFrameList);

                foreach (var itemIndex in beginFrameList)
                {
                    FrameBegin?.Invoke(currentExecuteTagName, itemIndex);
                }
                foreach (var itemIndex in endFrameList)
                {
                    FrameEnd?.Invoke(currentExecuteTagName, itemIndex);
                }

                if (beginFrameList.Count > 0)
                    CurrentFrameIndex = beginFrameList[beginFrameList.Count - 1];

                // add the remain ....
                TimeElapsed += dt;
            }
            else
            {
                var lastFrameIndex = currentTag.Frames.Count - 1;
                if (CurrentFrameIndex == lastFrameIndex && TimeElapsed > currentTag.Duration)
                {
                    // do nothing ...

                }
                else
                {
                    var curFrame = CurrentFrameIndex;
                    ElapsedTimeMoveFrame_NoLoopMode(CurrentFrameIndex, TimeElapsed, dt,
                        out var beginFrameList, out var endFrameList);

                    foreach (var itemIndex in beginFrameList)
                    {
                        FrameBegin?.Invoke(currentExecuteTagName, itemIndex);
                    }
                    foreach (var itemIndex in endFrameList)
                    {
                        FrameEnd?.Invoke(currentExecuteTagName, itemIndex);
                    }

                    // add the remain ....
                    if (beginFrameList.Count > 0)
                        CurrentFrameIndex = beginFrameList[beginFrameList.Count - 1];

                    TimeElapsed += dt;
                }

            }
        }

        /// <summary>
        /// invoked when frame is end
        /// </summary>
        public event Action<string, int> FrameEnd;
        /// <summary>
        /// invoked when frame is begin
        /// </summary>
        public event Action<string, int> FrameBegin;


        void ElapsedTimeMoveFrame_LoopMode(int startFrame, float startTime, float dt,
            out List<int> beginFrameResultList, out List<int> endFrameResultList)
        {
            // TODO:
            // 可以通过取余优化
            var beginFrameList = new List<int>(currentTag.Frames.Count);
            var endFrameList = new List<int>(currentTag.Frames.Count);

            // 变量获取
            var elapsedList = currentTag.ElapsedTimeList;
            var frameList = currentTag.Frames;
            var frameTotalCount = currentTag.Frames.Count;

            // 帧首部探测
            var espt = elapsedList[startFrame] - (startTime % currentTag.Duration);
            if (espt < 0)
                espt = 0.1f; // for no error

            dt -= espt;
            if (dt > 0) // 越过首帧，时间增量减去最初的一个 frame 时间
            {
                // 增加一帧
                int currentPassedFrameIndex = (startFrame + 1) % frameTotalCount;

                // dt > 0，确认越过一帧
                while (dt > 0)
                {
                    beginFrameList.Add(currentPassedFrameIndex);
                    endFrameList.Add((frameTotalCount + currentPassedFrameIndex - 1) % frameTotalCount); // prevent -1 get

                    currentPassedFrameIndex = (currentPassedFrameIndex + 1) % frameTotalCount;
                    espt = frameList[currentPassedFrameIndex].duration;
                    if (espt < 0)
                        espt = 0.1f; // for no error

                    dt -= espt;
                }
            }

            // give result
            beginFrameResultList = beginFrameList;
            endFrameResultList = endFrameList;
        }



        void ElapsedTimeMoveFrame_NoLoopMode(int startFrame, float startTime, float dt, 
            out List<int> beginFrameResultList, out List<int> endFrameResultList)
        {
            //var odd = endTime % currentTag.Duration;
            //var total = endTime - odd;
            //List<RenderableFrame> list = new List<RenderableFrame>();
            //list.AddRange();
            //Mathf.RoundToInt();

            // TODO:
            // 可以通过取余优化
            var beginFrameList = new List<int>(currentTag.Frames.Count);
            var endFrameList = new List<int>(currentTag.Frames.Count);

            // 变量获取
            var elapsedList = currentTag.ElapsedTimeList;
            var frameList = currentTag.Frames;
            var frameTotalCount = currentTag.Frames.Count;

            // 帧首部探测
            var espt = elapsedList[startFrame] - (startTime % currentTag.Duration);
            if (espt < 0) espt = 0.1f; // for no error
            dt -= espt;

            if (dt > 0) // 越过首帧，时间增量减去最初的一个 frame 时间
            {
                // 增加一帧
                int currentPassedFrameIndex = (startFrame + 1) % frameTotalCount;

                // dt > 0，确认越过一帧
                // currentPassedFrameIndex > startFrame 确认不能循环到前一帧
                while (dt > 0 && currentPassedFrameIndex > startFrame)
                {
                    beginFrameList.Add(currentPassedFrameIndex);
                    endFrameList.Add((frameTotalCount + currentPassedFrameIndex - 1) % frameTotalCount); // prevent -1 get

                    currentPassedFrameIndex = (currentPassedFrameIndex + 1) % frameTotalCount;
                    espt = frameList[currentPassedFrameIndex].duration;
                    if (espt < 0)
                        espt = 0.1f; // for no error

                    dt -= espt;
                }

                // 尾部重确认
                if (dt > 0 && (currentPassedFrameIndex > startFrame) == false)
                {
                    endFrameList.Add(currentTag.Frames.Count - 1);
                }
            }

            // give result
            beginFrameResultList = beginFrameList;
            endFrameResultList = endFrameList;
        }


        public void Pause() => IsPaused = true;
        public void Play() => IsPaused = false;

        /// <summary>
        /// Stops the animation (pause it then return to first frame or last if specified)
        /// </summary>
        public void Stop(bool onLast = false)
        {
            IsPaused = true;
            SetFrameImmediately(onLast ? currentTag.Frames.Count - 1 : 0);
        }
    }

}
