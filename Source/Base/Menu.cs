// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


/// <summary>
/// A simple menu system that supports keyboard and gamepad navigation.
/// Uses state-based selection rather than events.
/// </summary>
public class Menu
{
    private readonly List<MenuItem> _menuItems;
    private int _selectedIndex;
    private float _inputCooldown;
    private Action _cancelAction; // Action to perform on cancel (e.g., exit menu)
    
    // Oscillating scale properties
    private float _scaleTimer;

    private SoundEffect _selectSound;
    private SoundEffect _clickSound;

    public float ItemSpacing { get; set; } = 50f;
    public SpriteFont Font { get; set; }

    // State properties
    public int SelectedIndex => _selectedIndex;
    public MenuItem SelectedItem => _menuItems.Count > 0 ? _menuItems[_selectedIndex] : null;
    public int ItemCount => _menuItems.Count;
    public bool HasItems => _menuItems.Count > 0;

    public Color TextColor { get; set; } = new Color(255,182,0);
    public Color SelectedColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Initializes a new instance of the Menu class.
    /// </summary>
    /// <param name="font">The font used to render menu items.</param>
    /// <param name="cancelAction">Optional action to perform when the menu is canceled by hitting the Escape Key.</param>
    public Menu(SpriteFont font, ContentManager content, Action cancelAction = null)
    {
        _menuItems = new List<MenuItem>();
        Font = font;

        _selectSound = content.Load<SoundEffect>("Sounds/select");
        _clickSound = content.Load<SoundEffect>("Sounds/click");

        _selectedIndex = 0;
        _cancelAction = cancelAction ?? (() => { /* Default cancel action */ });
        _scaleTimer = 0f;
    }

    /// <summary>
    /// Adds a menu item to the menu.
    /// </summary>
    public void AddItem(string text, Action action)
    {
        var item = new MenuItem(text);
        _menuItems.Add(item);
        item.Action = action;

        // If this is the first item, select it
        if (_menuItems.Count == 1)
        {
            _selectedIndex = 0;
            UpdateSelectionHighlight();
        }
    }

    /// <summary>
    /// Adds a menu item to the menu.
    /// </summary>
    public void AddItem(MenuItem item)
    {
        _menuItems.Add(item);
        
        // If this is the first item, select it
        if (_menuItems.Count == 1)
        {
            _selectedIndex = 0;
            UpdateSelectionHighlight();
        }
    }

    /// <summary>
    /// Removes a menu item by index.
    /// </summary>
    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= _menuItems.Count)
            return false;

        _menuItems.RemoveAt(index);

        // Adjust selected index if necessary
        if (_menuItems.Count == 0)
        {
            _selectedIndex = 0;
        }
        else if (_selectedIndex >= _menuItems.Count)
        {
            _selectedIndex = _menuItems.Count - 1;
        }

        UpdateSelectionHighlight();
        return true;
    }

    /// <summary>
    /// Removes a menu item by reference.
    /// </summary>
    public bool RemoveItem(MenuItem item)
    {
        int index = _menuItems.IndexOf(item);
        return index >= 0 && RemoveItemAt(index);
    }

    /// <summary>
    /// Removes all menu items.
    /// </summary>
    public void Clear()
    {
        _menuItems.Clear();
        _selectedIndex = 0;
    }

    public void Activate()
    {
        // Ignore input when activated so we 
        // don't accidentally trigger actions on menu open
        _inputCooldown =  GameConstants.INPUT_COOLDOWN_TIME;
        _selectedIndex = 0; // Reset selection when activated
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// Gets a menu item by index.
    /// </summary>
    public MenuItem GetItem(int index)
    {
        if (index < 0 || index >= _menuItems.Count)
            return null;
        return _menuItems[index];
    }

    /// <summary>
    /// Sets the selected index directly.
    /// </summary>
    public void SetSelectedIndex(int index)
    {
        if (index < 0 || index >= _menuItems.Count)
            return;

        _selectedIndex = index;
        UpdateSelectionHighlight();
    }

    /// <summary>
    /// Updates the menu's input handling and navigation.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_menuItems.Count == 0)
        {
            return;
        }

        // Update input cooldown
        if (_inputCooldown > 0)
        {
            _inputCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Update scale timer for oscillating effect
        _scaleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * GameConstants.SCALE_SPEED;

        // Check for navigation input (only if cooldown has expired)
        if (_inputCooldown <= 0)
        {
            bool navigationInput = false;

            // Check for up/down navigation
            bool upPressed =    InputState.IsKeyPressed(Keys.Up) ||
                                //InputState.IsKeyPressed(Keys.W) ||
                                InputState.IsButtonPressed(Buttons.DPadUp) ||
                                InputState.IsButtonPressed(Buttons.LeftThumbstickUp);

            bool downPressed =  InputState.IsKeyPressed(Keys.Down) ||
                                //InputState.IsKeyPressed(Keys.S) ||
                                InputState.IsButtonPressed(Buttons.DPadDown) ||
                                InputState.IsButtonPressed(Buttons.LeftThumbstickDown);

            if (upPressed)
            {
                MovePrevious();
                navigationInput = true;
            }
            else if (downPressed)
            {
                MoveNext();
                navigationInput = true;
            }

            if (navigationInput)
            {
                _inputCooldown = GameConstants.INPUT_COOLDOWN_TIME;
            }

            // Check for confirm/cancel input
            var menuConfirmPressed = InputState.IsKeyPressed(Keys.Enter) ||
                               //InputState.IsKeyPressed(Keys.Space) ||
                               InputState.IsButtonPressed(Buttons.A);

            var menuCancelPressed = InputState.IsKeyPressed(Keys.Escape) ||
                                    InputState.IsButtonPressed(Buttons.B) ||
                                    InputState.IsButtonPressed(Buttons.Back);

            if (menuConfirmPressed)
            {
                // Execute the action of the selected item
                PlayClick();
                SelectedItem?.Action?.Invoke();                
            }
            if (menuCancelPressed)
            {
                // Handle cancel action, e.g., go back to previous menu or exit
                // This could be customized based on your game logic
                _cancelAction?.Invoke();
            }

        }
    }

    /// <summary>
    /// Moves to the next menu item.
    /// </summary>
    public void MoveNext()
    {
        if (_menuItems.Count == 0) return;

        _selectedIndex = (_selectedIndex + 1) % _menuItems.Count;
        UpdateSelectionHighlight();
        PlaySelect();
    }

    /// <summary>
    /// Moves to the previous menu item.
    /// </summary>
    public void MovePrevious()
    {
        if (_menuItems.Count == 0) return;

        _selectedIndex = (_selectedIndex - 1 + _menuItems.Count) % _menuItems.Count;
        UpdateSelectionHighlight();
        PlaySelect();
    }

    /// <summary>
    /// Updates the selection highlight on menu items.
    /// </summary>
    private void UpdateSelectionHighlight()
    {
        for (int i = 0; i < _menuItems.Count; i++)
        {
            _menuItems[i].IsSelected = (i == _selectedIndex);
        }
    }

    /// <summary>
    /// Calculates the current scale for the selected menu item based on oscillation.
    /// </summary>
    private float GetSelectedItemScale()
    {
        var normalizedSin = (MathF.Sin(_scaleTimer) + 1.0f) * 0.5f; // Normalize sin wave to 0-1
        return MathHelper.Lerp(GameConstants.MIN_SCALE, GameConstants.MAX_SCALE, normalizedSin);
    }

    /// <summary>
    /// Renders the menu to the screen.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (Font == null || _menuItems.Count == 0)
            return;

        for (int i = 0; i < _menuItems.Count; i++)
        {
            var item = _menuItems[i];
            var itemPosition = new Vector2(0, i * ItemSpacing);

            if (item.IsSelected)
            {
                // Apply oscillating scale to selected item
                var scale = GetSelectedItemScale();
                var textSize = Font.MeasureString(item.Text);
                var origin = textSize * 0.5f; // Center the scaling
                var scaledPosition = itemPosition + origin; // Adjust position to account for origin

                spriteBatch.DrawString(Font, item.Text, scaledPosition, SelectedColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Draw normal item without scaling
                spriteBatch.DrawString(Font, item.Text, itemPosition, TextColor);
            }
        }
    }

    /// <summary>
    /// Gets the total height of the menu.
    /// </summary>
    public float GetMenuHeight()
    {
        if (_menuItems.Count == 0) return 0f;
        return (_menuItems.Count - 1) * ItemSpacing + (Font?.MeasureString(_menuItems[0].Text).Y ?? 0);
    }

    /// <summary>
    /// Gets the maximum width of all menu items.
    /// </summary>
    public float GetMenuWidth()
    {
        if (Font == null || _menuItems.Count == 0) return 0f;

        float maxWidth = 0f;
        foreach (var item in _menuItems)
        {
            var textSize = Font.MeasureString(item.Text);
            if (textSize.X > maxWidth)
                maxWidth = textSize.X;
        }
        return maxWidth;
    }

    public void PlayClick()
    {
        if (_clickSound != null)
            _clickSound.Play(0.25f, 0, 0);
    }

    public void PlaySelect()
    {
        if (_selectSound != null)
            _selectSound.Play(0.25f, 0, 0);
    }
}
