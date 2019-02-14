using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Block_Buster
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class AudioManager : Microsoft.Xna.Framework.GameComponent
    {

        Song[] GameMusic;
        Song TitleMusic;
        SoundEffect[] SoundEffects;
        int currentSong = -1;
        Random rand = new Random();

        byte state = 0;
        /// <summary>
        /// 0: not playing
        /// 1: main menu
        /// 2: game
        /// </summary>
        public byte State {
            get
            {
                return state;
            }
            set
            {
                if (state != value)
                {
                    if (value == 2)
                    {
                        currentSong = -1;
                        NextSong();
                    }
                    else if (value == 1)
                        MediaPlayer.Play(TitleMusic);
                    else
                        MediaPlayer.Stop();
                    state = value;
                }
            }
        }

        public AudioManager(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            SoundEffects = new SoundEffect[5];
            SoundEffects[0] = Game.Content.Load<SoundEffect>(@"Sounds\hit1");
            SoundEffects[1] = Game.Content.Load<SoundEffect>(@"Sounds\hit2");
            SoundEffects[2] = Game.Content.Load<SoundEffect>(@"Sounds\fall");
            SoundEffects[3] = Game.Content.Load<SoundEffect>(@"Sounds\upgrade");
            SoundEffects[4] = Game.Content.Load<SoundEffect>(@"Sounds\bump");
            GameMusic = new Song[5];
            for (int i = 0; i < GameMusic.Length; i++)
                GameMusic[i] = Game.Content.Load<Song>(String.Format(@"Music\music_{0}", i + 1));
            TitleMusic = Game.Content.Load<Song>(@"Music\1minute");

            MediaPlayer.IsRepeating = true;
            base.Initialize();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    MediaPlayer.Stop();
                    TitleMusic.Dispose();
                    for (int i = 0; i < GameMusic.Length; i++)
                        GameMusic[i].Dispose();
                    for (int i = 0; i < SoundEffects.Length; i++)
                        SoundEffects[i].Dispose();
                }
                catch { }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (Game.IsActive)
            {
                if (MediaPlayer.State == MediaState.Paused)
                    MediaPlayer.Resume();
            }
            else if (MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();
            base.Update(gameTime);
        }

        public void NextSong()
        {
            int r = rand.Next(0, GameMusic.Length);
            while (r == currentSong)
                r = rand.Next(0, GameMusic.Length);
            MediaPlayer.Play(GameMusic[r]);
            currentSong = r;
        }
        public void PlayEffect(int index)
        {
            try
            {
                SoundEffects[index].Play();
            }
            catch
            {
            }

        }
    }
}
