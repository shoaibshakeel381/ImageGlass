﻿/*
 * imaeg - generic image utility in C#
 * Copyright (C) 2010  ed <tripflag@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License v2
 * (version 2) as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, refer to the following URL:
 * http://www.gnu.org/licenses/old-licenses/gpl-2.0.txt
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ImageGlass.Core
{
    public class ImgMan
    {
        private List<Img> lstImage; //image list
        private List<Img> lstQueue; //loading image queue
        private bool isErrorImage = false;

        public delegate void FinishLoadingImageHandler(object sender, EventArgs e);
        public event FinishLoadingImageHandler OnFinishLoadingImage;

        public bool IsErrorImage
        {
            get { return isErrorImage; }
            set { isErrorImage = value; }
        }

        public ImgMan()
        {
            lstImage = new List<Img>();
            lstQueue = new List<Img>();
        }
		
        public ImgMan(string[] filenames)
        {
            lstImage = new List<Img>();
            lstQueue = new List<Img>();

            foreach (string name in filenames)
			{
                lstImage.Add(new Img(name));
			}

            Thread tLoader = new Thread(new ThreadStart(Loader));
            tLoader.Priority = ThreadPriority.BelowNormal;
            tLoader.IsBackground = true;
            tLoader.Start();
        }


        /// <summary>
        /// Add a new image file to list
        /// </summary>
        /// <param name="filename"></param>
        public void AddItem(string filename)
        {
            lstImage.Add(new Img(filename));
        }


        /// <summary>
        /// Returns image i, applying all configured enhancements
        /// </summary>
        /// <param name="i">The image to return</param>
        /// <param name="isSkippingCache">Option to skip the cache</param>
        /// <returns>Image i</returns>
        public Image GetImage(int i, bool isSkippingCache = false)
        {
            Image img = null;

            if (!isSkippingCache)
            {
                // Start off with unloading excessive images
                for (int a = 0; a < i - 2; a++)
                {
                    lstImage[a].Dispose();
                }
                for (int a = i + 2; a < lstImage.Count; a++)
                {
                    lstImage[a].Dispose();
                }

                lstQueue.Clear();
                lstQueue.Add(lstImage[i]);
                Enqueue(i + 1);
                Enqueue(i - 1);
            }
            else
            {
                lstImage[i].Load();
            }

            while (!lstImage[i].IsFinished)
            {
                Thread.Sleep(1);
            }

			if (lstImage[i].IsFailed)
            {
                isErrorImage = true;
                img = new Bitmap(1, 1);
            }
            else
            {
                isErrorImage = false;
                img = lstImage[i].Get();
            }

            // Make sure someone is listening to event
            OnFinishLoadingImage?.Invoke(this, new EventArgs());

            return img;
        }

        /// <summary>
        /// Enqueue image i at a lower priority (caching)
        /// </summary>
        /// <param name="i"></param>
        private void Enqueue(int i)
        {
            if (i < 0 || i >= lstImage.Count) return;
            if (!lstImage[i].IsFinished)
            {
                lstQueue.Add(lstImage[i]);
            }
        }

        /// <summary>
        /// Worker thread; loads images.
        /// </summary>
        private void Loader()
        {
            while (true)
            {
                if (lstQueue.Count > 0)
                {
                    Img i = lstQueue[0];
                    lstQueue.RemoveAt(0);

                    if (!i.IsFinished)
                    {
                        i.Load();
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public int Length
        {
            get { return lstImage.Count; }
            set { }
        }

        /// <summary>
        /// Get full path of this image
        /// </summary>
        /// <returns>Full path of image</returns>
        public string GetFileName(int i)
        {
            try
            {
                return lstImage[i].GetFileName();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Get file list
        /// </summary>
        /// <returns></returns>
        public List<string> GetFileList()
        {
            var list = new List<string>();

            foreach(var item in lstImage)
            {
                list.Add(item.GetFileName());
            }

            return list;
        }

        /// <summary>
        /// Set full path of this image
        /// </summary>
        /// <param name="i">image index</param>
        /// <param name="s">new full path</param>
        public void SetFileName(int i, string s)
        {
            lstImage[i].SetFileName(s);
        }
        

        public void Unload(int i)
        {
            if (lstImage[i] != null)
                lstImage[i].Dispose();
        }

        public void Remove(int i)
        {
            Unload(i);
            lstImage.RemoveAt(i);
        }

        public void Remove(string filename)
        {
            int index = IndexOf(filename);
            Remove(index);
        }

        public int IndexOf(string filename)
        {
            // case sensitivity, esp. if filename passed on command line
            return lstImage.FindIndex(v => v.GetFileName().ToLower() == filename.ToLower());
        }

		public void Dispose()
        {
            for (int i = 0; i < Length; i++)
            {
                Remove(i);
            }
            lstImage.Clear();
            lstQueue.Clear();
        }
    }
}
