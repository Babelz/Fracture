using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Ui.Components;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ui.Controls
{
    public sealed class MessageBoxEventArgs : EventArgs
    {
        #region Properties
        public MessageBoxResult Result
        {
            get;
        }
        #endregion

        public MessageBoxEventArgs(MessageBoxResult result) => Result = result;
    }

    [Flags]
    public enum MessageBoxButtons : ushort
    {
        None = (1 << 0),
        Accept = (1 << 1),
        Ok = (1 << 2),
        Cancel = (1 << 3),
        Return = (1 << 4),
        Decline = (1 << 5),
        Close = (1 << 6),
        Retry = (1 << 7),
        Yes = (1 << 8),
        No = (1 << 9)
    }

    public enum MessageBoxResult : ushort
    {
        None = 0,
        Accept = 1,
        Ok = 2,
        Cancel = 3,
        Return = 4,
        Decline = 5,
        Close = 6,
        Retry = 7,
        Yes = 8,
        No = 9
    }

    public sealed class MessageBox : StaticContainerControl
    {
        #region Static fields
        public static readonly Vector2 DefaultSize = new Vector2(0.35f, 0.25f);
        #endregion

        #region Fields
        private readonly IDynamicContainerControl container;

        private readonly Paragraph messageBoxMessageParagraph;
        #endregion

        #region Properties
        public string Message
        {
            get => messageBoxMessageParagraph.Text;
            set => messageBoxMessageParagraph.Text = value;
        }
        #endregion

        #region Events
        public event EventHandler<MessageBoxEventArgs> Closed;
        #endregion

        private MessageBox(IDynamicContainerControl container,
                           string header,
                           string message,
                           MessageBoxButtons buttons,
                           bool draggable,
                           Vector2 size)
            : base(new ControlManager())
        {
            this.container = container;

            Size        = new Vector2(1.0f);
            Anchor      = Anchor.Top | Anchor.Left;
            Positioning = Positioning.Anchor;

            messageBoxMessageParagraph = new Paragraph
            {
                Positioning = Positioning.Anchor,
                Anchor      = Anchor.Center,
                Text        = message,
                Size        = new Vector2(0.9f, 0.4f),
                WrapAround  = true
            };

            var messageBoxPanel = new HeaderPanel
            {
                Positioning = Positioning.Offset,
                Anchor      = Anchor.Center,
                Size        = size,
                HeaderText  = header
            };

            if (draggable)
                messageBoxPanel.EnableDrag();

            // Create message box buttons.
            var messageBoxButtons = new List<Button>();

            if (buttons != MessageBoxButtons.None)
            {
                var messageBoxButtonsValues = Enum.GetValues(typeof(MessageBoxButtons))
                                                  .Cast<MessageBoxButtons>()
                                                  .ToList();

                var messageBoxResultValues = Enum.GetValues(typeof(MessageBoxResult))
                                                 .Cast<MessageBoxResult>()
                                                 .ToDictionary(v => v.ToString().ToLower(), v => v);

                foreach (var messageBoxButtonValue in messageBoxButtonsValues)
                {
                    if ((buttons & messageBoxButtonValue) == messageBoxButtonValue)
                        CreateButton(messageBoxButtons,
                                     messageBoxButtonValue.ToString().ToLower(),
                                     messageBoxResultValues[messageBoxButtonValue.ToString().ToLower()]);
                }
            }

            // Validate buttons.
            if (messageBoxButtons.Count != 0)
            {
                const int MaxMessageBoxButtons = 3;

                switch (messageBoxButtons.Count)
                {
                    // Align buttons.
                    case 1:
                        messageBoxButtons[0].Margin = UiOffset.ToBottom(0.025f);
                        break;
                    case 2:
                        messageBoxButtons[0].Margin = UiOffset.ToRight(0.15f) + UiOffset.ToBottom(0.025f);
                        messageBoxButtons[1].Margin = UiOffset.ToLeft(0.15f) + UiOffset.ToBottom(0.025f);
                        break;
                    case 3:
                        messageBoxButtons[0].Margin = UiOffset.ToRight(0.30f) + UiOffset.ToBottom(0.025f);
                        messageBoxButtons[1].Margin = UiOffset.ToBottom(0.025f);
                        messageBoxButtons[2].Margin = UiOffset.ToLeft(0.30f) + UiOffset.ToBottom(0.025f);
                        break;
                    default:
                        throw new InvalidOperationException($"max {MaxMessageBoxButtons} buttons are supported");
                }

                foreach (var button in messageBoxButtons)
                {
                    button.Margin += UiOffset.ToBottom(0.025f);

                    // Add buttons.
                    messageBoxPanel.Add(button);
                }
            }

            // Disable underlying container.
            for (var i = 0; i < container.ControlsCount; i++)
                container[i].Disable();

            // Show the message box.
            messageBoxPanel.Add(messageBoxMessageParagraph);

            Children.Add(messageBoxPanel);
        }

        private void CreateButton(ICollection<Button> buttons, string text, MessageBoxResult result)
        {
            var button = new Button
            {
                Id          = $"message-box-button-{text}",
                Size        = new Vector2(0.25f, 0.1f),
                Positioning = Positioning.Anchor,
                Anchor      = Anchor.Center | Anchor.Bottom,
                Text        = text
            };

            button.Click += (s, e) => Close(result);

            buttons.Add(button);
        }

        public void Close(MessageBoxResult result = MessageBoxResult.None)
        {
            var collection = (IControlCollection)container;

            collection.Remove(this);

            for (var i = 0; i < container.ControlsCount; i++)
                container[i].Enable();

            Closed?.Invoke(this, new MessageBoxEventArgs(result));
        }

        public static MessageBox Show(IDynamicContainerControl container,
                                      string message,
                                      string header = "",
                                      MessageBoxButtons buttons = MessageBoxButtons.None,
                                      bool draggable = false)
        {
            var messageBox = new MessageBox(container,
                                            header,
                                            message,
                                            buttons,
                                            draggable,
                                            DefaultSize);

            container.Add(messageBox);

            return messageBox;
        }

        public static MessageBox Show(IDynamicContainerControl container,
                                      Vector2 size,
                                      string message,
                                      string header = "",
                                      MessageBoxButtons buttons = MessageBoxButtons.None,
                                      bool draggable = false)
        {
            var messageBox = new MessageBox(container,
                                            header,
                                            message,
                                            buttons,
                                            draggable,
                                            size);

            container.Add(messageBox);

            return messageBox;
        }
    }
}