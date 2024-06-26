﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Chessed.src;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Chessed
{
    [Activity(Label = "GameScreen", ScreenOrientation = ScreenOrientation.Portrait)]
    public class GameScreen : Activity
    {
        readonly string abc = "abcdefgh";
        GridLayout chessBoard;
        private GameState gameState;
        private Position selectedPos = null;


        bool gameOver = false;

        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        ClientWebSocket client = Client.Instance.client;

        Player player = Player.White;

        Dictionary<string, string> matchData;

        TextView opponentName, playerName;
        TextView opponentElo, playerElo;

        Move lastMove = null;


        //Piece.PieceColor playingAs = Piece.PieceColor.White;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.chess_board);

            //board = new Board();
            gameState = new GameState(Player.White, Board.Initial());

            chessBoard = FindViewById<GridLayout>(Resource.Id.chessBoard);

            matchData = JsonSerializer.Deserialize<Dictionary<string, string>>(Intent.GetStringExtra("data"));

            

            // Set the details of each player
            opponentName = FindViewById<TextView>(Resource.Id.opponentName);
            opponentElo = FindViewById<TextView>(Resource.Id.opponentElo);

            playerName = FindViewById<TextView>(Resource.Id.playerName);
            playerElo = FindViewById<TextView>(Resource.Id.playerElo);

            opponentName.Text = matchData["playerName"];
            opponentElo.Text = matchData["playerElo"] + $" {GetText(Resource.String.elo)}";

            playerName.Text = Preferences.Get("username", "");
            playerElo.Text = Preferences.Get("elo", "") + $" {GetText(Resource.String.elo)}";

            player = matchData["color"] == "white" ? Player.White : Player.Black;
            HighlightCurrentPlayer();

            BuildBoard();
            DrawBoard(gameState.Board);

            Task.Run(async () => await ReadMessage());
        }

        async Task ReadMessage()
        {
            while (client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var message = new ArraySegment<byte>(new byte[4096]);
                do
                {
                    result = await client.ReceiveAsync(message, CancellationToken.None);
                    if (result.MessageType != WebSocketMessageType.Text)
                        break;
                    var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                    string resStr = System.Text.Encoding.UTF8.GetString(messageBytes);
                    Dictionary<string, string> receivedMessage = JsonSerializer.Deserialize<Dictionary<string, string>>(resStr);

                    if (receivedMessage["type"] == "draw_decline")
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, GetText(Resource.String.draw_declined), ToastLength.Short).Show();
                        });
                    }

                    if (receivedMessage["type"] == "draw_offer")
                    {
                        RunOnUiThread(() =>
                        {
                            AlertDialog.Builder builder = new AlertDialog.Builder(this, Resource.Style.Dialog);

                            AlertDialog alert = builder.Create();
                            alert.SetTitle(GetText(Resource.String.draw_received));
                            alert.SetMessage(GetText(Resource.String.draw_accept));
                            alert.SetButton2(GetText(Resource.String.yes), (sender, e) =>
                            {
                                Task.Run(async () =>
                                {
                                    string json = JsonSerializer.Serialize(new { token = Preferences.Get("token", ""), type = "drawAccept", gameId = matchData["gameId"] });
                                    var byteMessage = System.Text.Encoding.UTF8.GetBytes(json);
                                    var segmnet = new ArraySegment<byte>(byteMessage);

                                    await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
                                });
                            });

                            alert.SetButton(GetText(Resource.String.no), (s, e) =>
                            {
                                Task.Run(async () =>
                                {
                                    string json = JsonSerializer.Serialize(new { token = Preferences.Get("token", ""), type = "drawDecline", gameId = matchData["gameId"] });
                                    var byteMessage = System.Text.Encoding.UTF8.GetBytes(json);
                                    var segmnet = new ArraySegment<byte>(byteMessage);

                                    await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
                                });
                            });

                            alert.SetCancelable(false);

                            alert.Show();
                        });
                    }

                    if (receivedMessage["type"] == "move" || receivedMessage["type"] == "game_over")
                    {
                        string strMove = receivedMessage["data"];
                        RunOnUiThread(() => {
                            if (strMove == "null") return;

                            string[] move = strMove.Split('-');
                            int[] sqStart = Board.PositionFromStr(move[0]);
                            Position posStart = new Position(sqStart[0], sqStart[1]);

                            int[] sqEnd = Board.PositionFromStr(move[1]);
                            Position posEnd = new Position(sqEnd[0], sqEnd[1]);

                            OnFromPositionSelected(posStart, false);

                            if (move.Length > 2)
                            {
                                OnToPositionSelected(posEnd, move[2] switch
                                {
                                    "q" => PieceType.Queen,
                                    "r" => PieceType.Rook,
                                    "n" => PieceType.Knight,
                                    "b" => PieceType.Bishop,
                                    _ => null
                                });
                            }
                            else OnToPositionSelected(posEnd);
                        });
                    }

                    if (receivedMessage["type"] == "game_over")
                    {
                        gameOver = true;
                        RunOnUiThread(() =>
                        {
                            FindViewById(Resource.Id.gameoverScreen).Visibility = ViewStates.Visible;
                            TextView reason = FindViewById<TextView>(Resource.Id.gameOverReason);

                            reason.Text = receivedMessage["winner"] switch
                            {
                                "white" => GetText(Resource.String.white_won_by),
                                "black" => GetText(Resource.String.black_won_by),
                                "draw" => GetText(Resource.String.game_drawn),
                                _ => ""
                            };

                            reason.Text += " ";

                            reason.Text += receivedMessage["reason"] switch
                            {
                                "checkmate" => GetText(Resource.String.checkmate),
                                "agreement" => GetText(Resource.String.by_agreement),
                                "resignation" => GetText(Resource.String.resignation),
                                _ => receivedMessage["reason"]
                            };

                            TextView elo = FindViewById<TextView>(Resource.Id.newElo);
                            int eloDiff = int.Parse(receivedMessage["newElo"]) - int.Parse(Preferences.Get("elo", "0"));
                            elo.Text = $"{GetText(Resource.String.new_elo)} {receivedMessage["newElo"]} ({(eloDiff >= 0 ? "+" : "")}{eloDiff})";

                            Preferences.Set("elo", receivedMessage["newElo"]);
                        });
                    }
                }
                while (!result.EndOfMessage);
            }
        }

        private void OnFromPositionSelected(Position pos, bool highlight = true)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);

            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                if (highlight) ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos, PieceType? promotioPiece = null)
        {
            selectedPos = null;
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
            {
                
                if (move.Type == MoveType.PawnPromotion)
                {
                    if (promotioPiece == null)
                        HandlePromotion(move.FromPos, move.ToPos);
                    else
                    {
                        HandleMove(new PawnPromotion(move.FromPos, move.ToPos, (PieceType)promotioPiece));
                    }
                }
                else
                {
                    HandleMove(move);
                }
            }
        }

        private void HandlePromotion(Position from, Position to)
        {
            View promotionScreen = FindViewById(Resource.Id.promotionScreen);
            promotionScreen.Visibility = ViewStates.Visible;

            LinearLayout pieceLayout = FindViewById<LinearLayout>(Resource.Id.pieces);

            PieceType[] pieceTypes = new PieceType[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
            for (int i = 0; i < pieceTypes.Length; i++)
            {
                ImageButton btn = (ImageButton)pieceLayout.GetChildAt(i);
                btn.SetImageDrawable(Resources.GetDrawable(PieceImages.GetImage(player, pieceTypes[i]), Theme));

                // Creating this temp variable because I still updates after setting the delegate
                int temp = i;
                EventHandler click = null;
                click = (sender, e) =>
                {
                    Move promotionMove = new PawnPromotion(from, to, pieceTypes[temp]);
                    HandleMove(promotionMove);
                    promotionScreen.Visibility = ViewStates.Gone;

                    // Unsubscribe from self
                    btn.Click -= click;
                };

                btn.Click += click;
            }
        }

        [Export("ResignBtn")]
        public void ResignBtn(View v)
        {
            if (gameOver) return;

            AlertDialog.Builder builder = new AlertDialog.Builder(this, Resource.Style.Dialog);

            AlertDialog alert = builder.Create();
            alert.SetTitle(GetText(Resource.String.resign));
            alert.SetMessage(GetText(Resource.String.resign_confirm));
            alert.SetButton2(GetText(Resource.String.yes), (sender, e) =>
            {
                Task.Run(async () => {
                    string json = JsonSerializer.Serialize(new { token = Preferences.Get("token", ""), type = "resign", gameId = matchData["gameId"] });
                    var byteMessage = System.Text.Encoding.UTF8.GetBytes(json);
                    var segmnet = new ArraySegment<byte>(byteMessage);

                    await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
                });
            });

            alert.SetButton(GetText(Resource.String.no), (s, e) => { });

            alert.Show();
        }


        [Export("OfferDrawBtn")]
        public void OfferDrawBtn(View v)
        {
            if (gameOver) return;

            AlertDialog.Builder builder = new AlertDialog.Builder(this, Resource.Style.Dialog);

            AlertDialog alert = builder.Create();
            alert.SetTitle(GetText(Resource.String.offer_draw));
            alert.SetMessage(GetText(Resource.String.draw_confirm));
            alert.SetButton2(GetText(Resource.String.yes), (sender, e) =>
            {
                Task.Run(async () => {
                    string json = JsonSerializer.Serialize(new { token = Preferences.Get("token", ""), type = "drawOffer", gameId = matchData["gameId"] });
                    var byteMessage = System.Text.Encoding.UTF8.GetBytes(json);
                    var segmnet = new ArraySegment<byte>(byteMessage);

                    await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
                });
            });

            alert.SetButton(GetText(Resource.String.no), (s, e) => { });

            alert.Show();
        }

        [Export("ExitBtn")]
        public void ExitBtn(View v)
        {
            Finish();
        }

        private void HandleMove(Move move)
        {
            FrameLayout square = (FrameLayout)chessBoard.GetChildAt(move.ToPos.Row * 8 + move.ToPos.Column);
            ImageButton piece = (ImageButton)square.GetChildAt(0);
            bool isEmptySquare = piece.Drawable == null;

            if (gameState.CurrentPlayer == player)
            {
                Task.Run(async () => {
                    string promotionStr = "";
                    if (move.Type == MoveType.PawnPromotion)
                    {
                        promotionStr += "-";

                        promotionStr += ((PawnPromotion)move).newType switch
                        {
                            PieceType.Queen => "q",
                            PieceType.Rook => "r",
                            PieceType.Knight => "n",
                            PieceType.Bishop => "b",
                            _ => ""
                        };
                    }

                    string json = JsonSerializer.Serialize(new { token = Preferences.Get("token", ""), move = $"{move.FromPos}-{move.ToPos}{promotionStr}", gameId = matchData["gameId"] });
                    var byteMessage = System.Text.Encoding.UTF8.GetBytes(json);
                    var segmnet = new ArraySegment<byte>(byteMessage);

                    await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
                });
            }

            gameState.MakeMove(move);
            lastMove = move;
            DrawBoard(gameState.Board);
            HighlightLastMove();
            HighlightCurrentPlayer();

            string moveStr = GenerateMovePgn(move, !isEmptySquare || move.Type == MoveType.EnPassant);
            if (gameState.CurrentPlayer == Player.Black)
            {
                moveStr = $"{gameState.moves}. {moveStr}";
            }

            TextView moves = (TextView)FindViewById<RelativeLayout>(Resource.Id.moves).GetChildAt(0);
            moves.Text += $" {moveStr}";

            if (gameState.Board.IsInCheck(gameState.CurrentPlayer))
            {
                Sounds.PlaySound(this, "check");
            }
            else if (move.Type == MoveType.PawnPromotion)
            {
                Sounds.PlaySound(this, "promote");
            }
            else if (move.Type == MoveType.CastleQS || move.Type == MoveType.CastleKS)
            {
                Sounds.PlaySound(this, "castle");
            }
            else if (isEmptySquare && move.Type != MoveType.EnPassant)
            {
                Sounds.PlaySound(this, "move");
            }
            else
            {
                Sounds.PlaySound(this, "capture");
            }
        }

        private string GenerateMovePgn(Move move, bool isCapture)
        {
            string moveStr = "";

            Piece p = gameState.Board[move.ToPos.Row, move.ToPos.Column];

            moveStr += p.Type switch
            {
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.King => "K",
                PieceType.Pawn => abc[move.FromPos.Column],
                _ => ""
            };

            // Without this, it will think that the piece is not a pawn, but the piece it was promoted to
            if (move.Type == MoveType.PawnPromotion) moveStr = abc[move.FromPos.Column].ToString();

            if (isCapture)
            {
                moveStr += "x";
            }
            
            if (p.Type == PieceType.Pawn)
            {
                if (isCapture) moveStr += abc[move.ToPos.Column];
                moveStr += 8 - move.ToPos.Row;
            }
            else
            {
                moveStr += abc[move.ToPos.Column];
                moveStr += 8 - move.ToPos.Row;
            }

            if (move.Type == MoveType.PawnPromotion)
            {
                moveStr += "=";
                moveStr += ((PawnPromotion)move).newType switch
                {
                    PieceType.Queen => "Q",
                    PieceType.Rook => "R",
                    PieceType.Knight => "N",
                    PieceType.Bishop => "B",
                    _ => ""
                };
            }

            if (move.Type == MoveType.CastleKS) moveStr = "O-O";
            if (move.Type == MoveType.CastleQS) moveStr = "O-O-O";

            if (gameState.Board.IsInCheck(gameState.CurrentPlayer))
            {
                if (gameState.IsGameOver())
                    moveStr += "#";
                else
                    moveStr += "+";
            }

            return moveStr;
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();

            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;
            }
        }

        private void ShowHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                ((ImageButton)((FrameLayout)chessBoard.GetChildAt(to.Row * 8 + to.Column)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(Resource.Color.board_select, Theme));
                //highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
            }
        }

        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                ((ImageButton)((FrameLayout)chessBoard.GetChildAt(to.Row * 8 + to.Column)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(((to.Row + to.Column) % 2 == 1) ? Resource.Color.board_dark : Resource.Color.board_light, Theme));
            }

            HighlightLastMove();
        }

        private void HighlightCurrentPlayer()
        {
            if (gameState.CurrentPlayer == player)
            {
                FindViewById<RelativeLayout>(Resource.Id.playerCard).SetBackgroundColor(Resources.GetColor(Resource.Color.green, Theme));
                FindViewById<RelativeLayout>(Resource.Id.opponentCard).SetBackgroundColor(Resources.GetColor(Resource.Color.bg_dark, Theme));
            }
            else
            {
                FindViewById<RelativeLayout>(Resource.Id.playerCard).SetBackgroundColor(Resources.GetColor(Resource.Color.bg_dark, Theme));
                FindViewById<RelativeLayout>(Resource.Id.opponentCard).SetBackgroundColor(Resources.GetColor(Resource.Color.green, Theme));
            }
        }

        private void HighlightLastMove()
        {
            // Reset board colors
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    ((ImageButton)((FrameLayout)chessBoard.GetChildAt(i * 8 + j)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(((i + j) % 2 == 1) ? Resource.Color.board_dark : Resource.Color.board_light, Theme));
                }
            }

            if (lastMove == null) return;

            ((ImageButton)((FrameLayout)chessBoard.GetChildAt(lastMove.FromPos.Row * 8 + lastMove.FromPos.Column)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(((lastMove.FromPos.Row + lastMove.FromPos.Column) % 2 == 1) ? Resource.Color.board_dark_moved : Resource.Color.board_light_moved, Theme));
            ((ImageButton)((FrameLayout)chessBoard.GetChildAt(lastMove.ToPos.Row * 8 + lastMove.ToPos.Column)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(((lastMove.ToPos.Row + lastMove.ToPos.Column) % 2 == 1) ? Resource.Color.board_dark_moved : Resource.Color.board_light_moved, Theme));

            Player checkedPlayer = Player.None;

            if (gameState.Board.IsInCheck(gameState.CurrentPlayer)) checkedPlayer = gameState.CurrentPlayer;
            if (gameState.Board.IsInCheck(gameState.CurrentPlayer.Opponent())) checkedPlayer = gameState.CurrentPlayer.Opponent();

            if (checkedPlayer == Player.None) return;

            Position kingPos = gameState.Board.FindPiece(checkedPlayer, PieceType.King);
            ((ImageButton)((FrameLayout)chessBoard.GetChildAt(kingPos.Row * 8 + kingPos.Column)).GetChildAt(0)).SetBackgroundColor(Resources.GetColor(Resource.Color.danger, Theme));
        }

        void BuildBoard()
        {
            int width = Resources.DisplayMetrics.WidthPixels;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    FrameLayout container = new FrameLayout(this)
                    {
                        LayoutParameters = new LinearLayout.LayoutParams(width / 8, width / 8),
                        Background = Resources.GetDrawable(Resource.Drawable.border, Theme)
                    };

                    ImageButton buttonView = new ImageButton(this)
                    {
                        LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
                    };
                    buttonView.SetScaleType(ImageView.ScaleType.FitCenter);
                    //buttonView.SetAdjustViewBounds(true);
                    buttonView.SetPadding(0, 0, 0, 0);
                    buttonView.Click += SquareClick;

                    TextView coordTv = new TextView(this)
                    {
                        LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
                        Gravity = GravityFlags.Left | GravityFlags.Bottom,
                    };

                    buttonView.SetBackgroundColor(Resources.GetColor((j + i) % 2 == 1 ? Resource.Color.board_dark : Resource.Color.board_light, Theme));

                    if (player == Player.White)
                    {
                        if (j != 0 && i == 7)
                        {
                            coordTv.Text = abc[j].ToString();
                        }

                        if (j == 0)
                        {
                            coordTv.Text = (8 - i).ToString();
                            if (i == 7)
                            {
                                coordTv.Text += "a";
                            }
                        }
                    }
                    else
                    {
                        if (j != 7 && i == 0)
                        {
                            coordTv.Text = abc[j].ToString();
                        }

                        if (j == 7)
                        {
                            coordTv.Text = (8 - i).ToString();
                            if (i == 0)
                            {
                                coordTv.Text += "h";
                            }
                        }
                    }

                    coordTv.SetPadding(10, 10, 10, 10);

                    buttonView.TransitionName = $"{i}{j}";

                    //buttonView.SetOnClickListener(this);

                    container.AddView(buttonView);
                    container.AddView(coordTv);

                    if (player == Player.Black) container.Rotation = 180;

                    chessBoard.AddView(container);
                }
            }
        }

        private void SquareClick(object sender, EventArgs e)
        {
            if (gameOver) return;
            if (gameState.CurrentPlayer != player) return;

            int[] sq = Board.PositionFromStr(((View)sender).TransitionName);
            Position pos = new Position(sq[0], sq[1]);

            if (selectedPos == null)
            {
                OnFromPositionSelected(pos);
            }
            else
            {
                OnToPositionSelected(pos);
            }
        }

        void DrawBoard(Board board)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    ImageButton ib = (ImageButton)((FrameLayout)chessBoard.GetChildAt(8 * i + j)).GetChildAt(0);

                    Piece piece = board[i, j];

                    if (PieceImages.GetImage(piece) != 0) ib.SetImageDrawable(Resources.GetDrawable(PieceImages.GetImage(piece), Theme));
                    else ib.SetImageDrawable(null);
                }
            }

            if (player == Player.Black)
            {
                chessBoard.Rotation = 180;
            }
        }

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back && e.Action == KeyEventActions.Down)
            {
                if (gameOver) Finish();
                else ResignBtn(null);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
    }
}