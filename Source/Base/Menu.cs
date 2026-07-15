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
/// A simple menu system that supports keyboard and GamePad navigation.
/// Uses state-based selection rather than events.
/// Supports animated transitions when activating and deactivating.
/// </summary>
public class Menu
{
    private readonly List<MenuItem> _menuItems;
    private int _selectedIndex;
    private float _inputCooldown;
    private Action _cancelAction; // Action to perform on cancel (e.g., exit menu)

    // Oscillating scale properties
    private float _scaleTimer;

    // Transition properties
    private MenuState _menuState;
    private MenuTransitionDirection _transitionDirection;
    private float _transitionTimer;
    private Vector2 _basePosition; // The final position when fully transitioned in
    private Action _onTransitionOutComplete; // Callback when transition out completes

    private SoundEffect _selectSound;
    private SoundEffect _clickSound;

    public float ItemSpacing { get; set; } = 80f;
    public SpriteFont Font { get; set; }

    // State properties
    public int SelectedIndex => _selectedIndex;
    public MenuItem SelectedItem => _menuItems.Count > 0 ? _menuItems[_selectedIndex] : null;
    public int ItemCount => _menuItems.Count;
    public bool HasItems => _menuItems.Count > 0;
    public MenuState CurrentState => _menuState;
    public bool IsInteractive => _menuState == MenuState.Active;

    public Color TextColor { get; set; } = new Color(255,182,0);
    public Color SelectedColor { get; set; } = Color.Yellow;

    // Transition properties
    public MenuTransitionDirection TransitionDirection
    {
        get => _transitionDirection;
        set => _transitionDirection = value;
    }

    public Vector2 BasePosition
    {
        get => _basePosition;
        set => _basePosition = value;
    }

    /// <summary>
    /// Initializes a new instance of the Menu class.
    /// </summary>
    /// <param name="font">The font used to render menu items.</param>
    /// <param name="content">Content manager for loading sounds.</param>
    /// <param name="cancelAction">Optional action to perform when the menu is canceled by hitting the Escape Key.</param>
    /// <param name="transitionDirection">Direction from which menu items will transition in.</param>
    public Menu(SpriteFont font, ContentManager content, Action cancelAction = null, MenuTransitionDirection transitionDirection = MenuTransitionDirection.Top)
    {
        _menuItems = new List<MenuItem>();
        Font = font;

        _selectSound = content.Load<SoundEffect>("Sounds/select");
        _clickSound = content.Load<SoundEffect>("Sounds/click");

        _selectedIndex = 0;
        _cancelAction = cancelAction ?? (() => { /* Default cancel action */ });
        _scaleTimer = 0f;

        // Initialize transition properties
        _menuState = MenuState.Hidden;
        _transitionDirection = transitionDirection;
        _transitionTimer = 0f;
        _basePosition = Vector2.Zero;
        _onTransitionOutComplete = null;
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
        _inputCooldown = GameConstants.INPUT_COOLDOWN_TIME;
        _selectedIndex = 0; // Reset selection when activated
        UpdateSelectionHighlight();

        // Start transition in
        _menuState = MenuState.TransitionIn;
        _transitionTimer = 0f;
    }

    /// <summary>
    /// Starts the transition out animation and calls the provided callback when complete.
    /// </summary>
    /// <param name="onComplete">Callback to execute when transition out completes.</param>
    public void Deactivate(Action onComplete = null)
    {
        _onTransitionOutComplete = onComplete;
        _menuState = MenuState.TransitionOut;
        _transitionTimer = 0f;
    }

    /// <summary>
    /// Immediately hides the menu without transition.
    /// </summary>
    public void Hide()
    {
        _menuState = MenuState.Hidden;
        _transitionTimer = 0f;
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
    /// Updates the menu's input handling, navigation, and transitions.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_menuItems.Count == 0)
        {
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update transition state
        UpdateTransition(deltaTime);

        // Update input cooldown
        if (_inputCooldown > 0)
        {
            _inputCooldown -= deltaTime;
        }

        // Update scale timer for oscillating effect
        _scaleTimer += deltaTime * GameConstants.SCALE_SPEED;

        // Only handle input if menu is fully active
        if (_menuState != MenuState.Active)
        {
            return;
        }

        // Mouse hover/click support.
        HandleMouseInput(InputState.MousePosition);

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
    /// Handles mouse hover and click interaction with menu items.
    /// </summary>
    private void HandleMouseInput(Vector2 uiMousePosition)
    {
        int hovered = GetItemIndexAtPosition(uiMousePosition);
        if (hovered < 0)
            return;

        if (InputState.MouseDelta != Vector2.Zero && hovered != _selectedIndex)
        {
            _selectedIndex = hovered;
            UpdateSelectionHighlight();
            PlaySelect();
        }

        if (_inputCooldown <= 0 && InputState.IsMouseButtonPressed(MouseButton.LeftButton))
        {
            if (hovered != _selectedIndex)
            {
                _selectedIndex = hovered;
                UpdateSelectionHighlight();
            }

            PlayClick();
            _menuItems[hovered].Action?.Invoke();
        }
    }

    /// <summary>
    /// Returns the index of the menu item under the given UI-space position, or -1 if none.
    /// </summary>
    private int GetItemIndexAtPosition(Vector2 position)
    {
        if (Font == null)
            return -1;

        float transitionProgress = GetTransitionProgress();
        for (int i = 0; i < _menuItems.Count; i++)
        {
            // Match exactly where Draw() places the item so the hit area
            // tracks the rendered text (including transition offsets).
            var itemBasePosition = _basePosition + new Vector2(0, i * ItemSpacing);
            var itemPosition = GetTransitionPosition(itemBasePosition, transitionProgress, i);
            var size = Font.MeasureString(_menuItems[i].Text);

            // A little vertical padding makes items easier to hover.
            float pad = ItemSpacing * 0.25f;
            if (position.X >= itemPosition.X && position.X <= itemPosition.X + size.X &&
                position.Y >= itemPosition.Y - pad && position.Y <= itemPosition.Y + size.Y + pad)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Updates the transition state and timer.
    /// </summary>
    private void UpdateTransition(float deltaTime)
    {
        switch (_menuState)
        {
            case MenuState.TransitionIn:
                _transitionTimer += deltaTime;
                if (_transitionTimer >= GameConstants.MENU_TRANSITION_DURATION)
                {
                    _transitionTimer = GameConstants.MENU_TRANSITION_DURATION;
                    _menuState = MenuState.Active;
                }
                break;

            case MenuState.TransitionOut:
                _transitionTimer += deltaTime;
                if (_transitionTimer >= GameConstants.MENU_TRANSITION_DURATION)
                {
                    _transitionTimer = GameConstants.MENU_TRANSITION_DURATION;
                    _menuState = MenuState.Hidden;
                    _onTransitionOutComplete?.Invoke();
                    _onTransitionOutComplete = null;
                }
                break;

            case MenuState.Hidden:
            case MenuState.Active:
                // No transition updates needed
                break;
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
    /// Renders the menu to the screen with transition effects.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (Font == null || _menuItems.Count == 0 || _menuState == MenuState.Hidden)
            return;

        // Calculate transition progress (0 = start, 1 = end)
        float transitionProgress = GetTransitionProgress();

        for (int i = 0; i < _menuItems.Count; i++)
        {
            var item = _menuItems[i];

            // Calculate base position for this item
            var itemBasePosition = _basePosition + new Vector2(0, i * ItemSpacing);

            // Apply transition offset
            var itemPosition = GetTransitionPosition(itemBasePosition, transitionProgress, i);

            // Calculate opacity based on transition progress
            float alpha = _menuState == MenuState.TransitionOut ? 1f - transitionProgress : transitionProgress;
            var textColor = item.IsSelected ? SelectedColor : TextColor;
            textColor *= alpha;

            if (item.IsSelected)
            {
                // Apply oscillating scale to selected item
                var scale = GetSelectedItemScale();
                var textSize = Font.MeasureString(item.Text);
                var origin = textSize * 0.5f; // Center the scaling
                var scaledPosition = itemPosition + origin; // Adjust position to account for origin

                spriteBatch.DrawString(Font, item.Text, scaledPosition, textColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Draw normal item without scaling
                spriteBatch.DrawString(Font, item.Text, itemPosition, textColor);
            }
        }
    }

    /// <summary>
    /// Gets the current transition progress (0.0 to 1.0).
    /// </summary>
    private float GetTransitionProgress()
    {
        float progress = _transitionTimer / GameConstants.MENU_TRANSITION_DURATION;
        return MathHelper.Clamp(progress, 0f, 1f);
    }

    /// <summary>
    /// Calculates the position of a menu item during transition.
    /// </summary>
    private Vector2 GetTransitionPosition(Vector2 finalPosition, float progress, int itemIndex)
    {
        if (_transitionDirection == MenuTransitionDirection.None)
            return finalPosition;

        // Apply easing to the progress for smoother animation
        float easedProgress = _menuState == MenuState.TransitionOut ?
            MathHelpers.EaseInCubic(progress) : MathHelpers.EaseOutCubic(progress);

        // Add staggered delay for each item (creates a cascade effect)
        float staggerDelay = itemIndex * 0.1f; // 0.1 second delay between items
        float adjustedProgress = MathHelper.Clamp(easedProgress - staggerDelay, 0f, 1f);

        if (_menuState == MenuState.TransitionOut)
            adjustedProgress = 1f - adjustedProgress;

        Vector2 offset = GetOffsetForDirection(_transitionDirection);
        return Vector2.Lerp(finalPosition + offset, finalPosition, adjustedProgress);
    }

    /// <summary>
    /// Gets the offset vector for the specified transition direction.
    /// </summary>
    private Vector2 GetOffsetForDirection(MenuTransitionDirection direction)
    {
        switch (direction)
        {
            case MenuTransitionDirection.Top:
                return new Vector2(0, -GameConstants.MENU_TRANSITION_OFFSET);
            case MenuTransitionDirection.Bottom:
                return new Vector2(0, GameConstants.MENU_TRANSITION_OFFSET);
            case MenuTransitionDirection.Left:
                return new Vector2(-GameConstants.MENU_TRANSITION_OFFSET, 0);
            case MenuTransitionDirection.Right:
                return new Vector2(GameConstants.MENU_TRANSITION_OFFSET, 0);
            default:
                return Vector2.Zero;
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

    /// <summary>
    /// Sets the transition direction for the menu.
    /// </summary>
    /// <param name="direction">The direction from which menu items will transition.</param>
    public void SetTransitionDirection(MenuTransitionDirection direction)
    {
        _transitionDirection = direction;
    }

    /// <summary>
    /// Checks if the menu is currently transitioning (in or out).
    /// </summary>
    public bool IsTransitioning => _menuState == MenuState.TransitionIn || _menuState == MenuState.TransitionOut;

    /// <summary>
    /// Checks if the menu is fully visible and ready for interaction.
    /// </summary>
    public bool IsFullyActive => _menuState == MenuState.Active;

    /// <summary>
    /// Gets the current transition progress as a percentage (0-100).
    /// </summary>
    public float TransitionProgressPercent => GetTransitionProgress() * 100f;
}
