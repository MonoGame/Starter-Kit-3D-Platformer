// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Handles screen transitions like fade in and fade out.
/// </summary>
public class TransitionProcessor
{
    public enum TransitionState
    {
        None,
        FadeOut,
        FadeIn
    }

    private TransitionState _transitionState = TransitionState.None;
    private float _transitionTimer = 0f;
    private float _transitionDuration = 0.33f;
    private Texture2D _overlayTexture;
    private SpriteBatch _spriteBatch;
    private Action _onStateChangeCallback;

    public bool IsTransitioning => _transitionState != TransitionState.None;
    public float TransitionDuration
    {
        get => _transitionDuration;
        set => _transitionDuration = MathHelper.Max(0.1f, value);
    }

    public TransitionProcessor(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _spriteBatch = spriteBatch;
        _overlayTexture = new Texture2D(graphicsDevice, 1, 1);
        _overlayTexture.SetData([Color.White]);
    }

    public void StartTransition(Action onStateChangeCallback = null)
    {
        if (_transitionState == TransitionState.None)
        {
            _transitionState = TransitionState.FadeOut;
            _transitionTimer = 0f;
            _onStateChangeCallback = onStateChangeCallback;
        }
    }

    public void Update(float deltaTime)
    {
        if (_transitionState == TransitionState.None)
            return;

        _transitionTimer += deltaTime;

        switch (_transitionState)
        {
            case TransitionState.FadeOut:
                if (_transitionTimer >= _transitionDuration)
                {
                    // Transition complete, start fade in and call state change callback
                    _transitionState = TransitionState.FadeIn;
                    _transitionTimer = 0f;
                    _onStateChangeCallback?.Invoke();
                    _onStateChangeCallback = null;
                }
                break;

            case TransitionState.FadeIn:
                if (_transitionTimer >= _transitionDuration)
                {
                    // Fade in complete
                    _transitionState = TransitionState.None;
                    _transitionTimer = 0f;
                }
                break;
        }
    }

    public float GetTransitionAlpha()
    {
        if (_transitionState == TransitionState.None)
            return 0f;

        float progress = MathHelper.Clamp(_transitionTimer / _transitionDuration, 0f, 1f);

        switch (_transitionState)
        {
            case TransitionState.FadeOut:
                return progress; // 0 to 1 (transparent to opaque)
            case TransitionState.FadeIn:
                return 1f - progress; // 1 to 0 (opaque to transparent)
            default:
                return 0f;
        }
    }

    public void DrawTransition(Vector2 scale, float baseResolutionWidth, float baseResolutionHeight)
    {
        if (!IsTransitioning)
            return;

        float alpha = GetTransitionAlpha();
        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(scale.X, scale.Y, 0f));
        _spriteBatch.Draw(_overlayTexture,
            new Rectangle(0, 0, (int)baseResolutionWidth, (int)baseResolutionHeight),
            Color.Black * alpha);
        _spriteBatch.End();
    }

    public void Dispose()
    {
        _overlayTexture?.Dispose();
    }
}
