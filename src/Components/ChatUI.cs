using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.GettingOverItMP.EventArgs;
using ServerShared;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Oxide.GettingOverItMP.Components
{
    public class ChatUI : MonoBehaviour
    {
        public class ChatMessage
        {
            public string Name;
            public string Message;
            public Color Color = Color.white;
            public DateTime Time;
        }

        public const int MaxChatMessages = 100;
        public static readonly Vector2 ChatSize = new Vector2(400, 300);

        public bool Writing { get; private set; }

        private Client client;
        private PlayerControl playerControl;
        private string chatInputText = "";
        private readonly List<ChatMessage> chatMessages = new List<ChatMessage>();
        private Vector2 scrollPosition = Vector2.zero;
        private float hideChatTime = 0; // Hide chat if the current time is higher than this.

        private static GUIStyle groupStyle;
        private static GUIStyle chatTextStyle;

        private const float chatShowTimeOnMessageReceived = 10; // Show the chat for 10 seconds when a message is received.
        private const float chatShowTimeOnMessageCancelled = 3; // Show the chat for 3 seconds when a message when chat input was cancelled.

        private void Start()
        {
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>() ?? throw new NotImplementedException("Could not find client");
            playerControl = GameObject.Find("Player").GetComponent<PlayerControl>() ?? throw new NotImplementedException("Could not find PlayerControl");

            client.ChatMessageReceived += OnChatMessageReceived;
        }

        private void Update()
        {
            if (!Writing && Input.GetKeyDown(KeyCode.T) && !playerControl.IsPaused())
            {
                Writing = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                playerControl.Pause();
            }
        }

        private void OnGUI()
        {
            if (groupStyle == null)
            {
                groupStyle = new GUIStyle(GUI.skin.GetStyle("Box"))
                {
                    normal =
                    {
                        background = Texture2D.whiteTexture
                    }
                };

                chatTextStyle = GUI.skin.GetStyle("Label");
                chatTextStyle.fontSize = 14;
            }

            if (playerControl.IsPaused() && !Writing)
                return;

            if (!Writing)
            {
                if (Time.time >= hideChatTime)
                    return;
            }

            // Draw chat in the bottom left corner 30 pixels above the bottom.
            {
                var bColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                GUI.BeginGroup(new Rect(5, Screen.height - 35 - ChatSize.y, ChatSize.x, ChatSize.y), (Texture) null, groupStyle);
                GUI.backgroundColor = bColor;
            }
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(ChatSize.x), GUILayout.Height(ChatSize.y));
                {
                    Color oldColor = GUI.color;

                    for (int i = 0; i < chatMessages.Count; ++i)
                    {
                        var chatMessage = chatMessages[i];
                        string namePrefix = "";

                        if (chatMessage.Name != null)
                            namePrefix = $"{chatMessage.Name}: ";

                        GUI.color = chatMessage.Color;
                        GUILayout.Label($"{namePrefix}{chatMessage.Message}");
                        GUILayout.Space(-3);
                    }

                    GUI.color = oldColor;
                }
                GUI.EndScrollView();
            }
            GUI.EndGroup();

            if (Writing)
            {
                GUI.SetNextControlName("ChatInput");
                chatInputText = GUI.TextField(new Rect(5, Screen.height - 30, ChatSize.x, 25), chatInputText, SharedConstants.MaxChatLength);
                GUI.FocusControl("ChatInput");

                var currentEvent = UnityEngine.Event.current;
                if (currentEvent.isKey && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
                {
                    if (chatInputText.Trim().Length > 0)
                    {
                        client.SendChatMessage(chatInputText);
                        ShowChat(chatShowTimeOnMessageReceived);
                    }
                    else
                    {
                        ShowChat(chatShowTimeOnMessageCancelled);
                    }

                    chatInputText = "";
                    Writing = false;
                    LockMouse();
                    ScrollToBottom();
                }
                else if (currentEvent.isKey && currentEvent.keyCode == KeyCode.Escape)
                {
                    chatInputText = "";
                    Writing = false;
                    ShowChat(chatShowTimeOnMessageCancelled);
                    LockMouse();
                    ScrollToBottom();
                }
            }
        }

        public void AddMessage(string message, string name, Color color)
        {
            ShowChat(chatShowTimeOnMessageReceived);

            if (chatMessages.Count > MaxChatMessages)
            {
                chatMessages.RemoveAt(0);
            }

            chatMessages.Add(new ChatMessage
            {
                Message = message,
                Name = name,
                Color = color,
                Time = DateTime.Now
            });

            ScrollToBottom();
        }

        private void OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs args)
        {
            AddMessage(args.Message, args.Player?.PlayerName, args.Color);
        }

        private void ScrollToBottom()
        {
            // Scroll to bottom of chat by setting scrollPosition.y to some value beyond the max.
            scrollPosition.y = 5000;
        }

        private void ShowChat(float time)
        {
            hideChatTime = Time.time + time;
        }

        private void LockMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerControl.UnPause();
        }
    }
}
