using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Service.Autofill;
using Android.Util;
using Android.Views;
using Android.Widget;
using Chessed.src;
using Java.Security.Cert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Android.Provider.MediaStore;

namespace Chessed
{
    [Activity(Label = "GameScreen")]
    public class GameScreen : Activity//, View.IOnClickListener
    {
        readonly string abc = "abcdefgh";
        GridLayout chessBoard;
        private GameState gameState;
        private Position selectedPos = null;


        //Board board;

        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        ClientWebSocket client = Client.Instance.client;

        const Player player = Player.Black;

        Dictionary<string, string> matchData;

        TextView opponentName, playerName;
        TextView opponentElo, playerElo;


        //Piece.PieceColor playingAs = Piece.PieceColor.White;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.chess_board);

            //board = new Board();
            gameState = new GameState(Player.White, Board.Initial());

            chessBoard = FindViewById<GridLayout>(Resource.Id.chessBoard);

            matchData = JsonSerializer.Deserialize<Dictionary<string, string>>(Intent.GetStringExtra("data"));

            BuildBoard();
            DrawBoard(gameState.Board);

            // Set the details of each player
            opponentName = FindViewById<TextView>(Resource.Id.opponentName);
            opponentElo = FindViewById<TextView>(Resource.Id.opponentElo);

            playerName = FindViewById<TextView>(Resource.Id.playerName);
            playerElo = FindViewById<TextView>(Resource.Id.playerElo);

            opponentName.Text = matchData["playerName"];
            opponentElo.Text = matchData["playerElo"] + " ELO";

            playerName.Text = Preferences.Get("username", "");
            playerElo.Text = Preferences.Get("elo", "") + " ELO";


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
                    result = await client.ReceiveAsync(message, default);
                    if (result.MessageType != WebSocketMessageType.Text)
                        break;
                    var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                    string receivedMessage = System.Text.Encoding.UTF8.GetString(messageBytes);
                    RunOnUiThread(() => {
                        Toast.MakeText(this, receivedMessage, ToastLength.Short).Show();

                        string[] move = receivedMessage.Split('-');
                        int[] sqStart = Board.PositionFromStr(move[0]);
                        Position posStart = new Position(sqStart[0], sqStart[1]);

                        int[] sqEnd = Board.PositionFromStr(move[1]);
                        Position posEnd = new Position(sqEnd[0], sqEnd[1]);

                        OnFromPositionSelected(posStart, false);
                        OnToPositionSelected(posEnd);
                    });
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

        private void OnToPositionSelected(Position pos)
        {
            selectedPos = null;
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
            {
                if (gameState.CurrentPlayer == player)
                {
                    Task.Run(async () => {
                        var byteMessage = System.Text.Encoding.UTF8.GetBytes($"{move.FromPos}-{move.ToPos}");
                        var segmnet = new ArraySegment<byte>(byteMessage);

                        await client.SendAsync(segmnet, WebSocketMessageType.Text, true, default);
                    });
                }

                if (move.Type == MoveType.PawnPromotion)
                {
                    HandlePromotion(move.FromPos, move.ToPos);
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

        private void HandleMove(Move move)
        {
            FrameLayout square = (FrameLayout)chessBoard.GetChildAt(move.ToPos.Row * 8 + move.ToPos.Column);
            ImageButton piece = (ImageButton)square.GetChildAt(0);
            bool isEmptySquare = piece.Drawable == null;

            gameState.MakeMove(move);
            DrawBoard(gameState.Board);

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
            //SetCursor(gameState.CurrentPlayer);

            if (gameState.IsGameOver())
            {
                Toast.MakeText(this, "GAME OVER", ToastLength.Short).Show();
            }
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
                //highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
            }
        }

        //public void OnClick(View v)
        //{
        //    Log.Debug("SCREEN", "PRESSED OUTER");

        //    //if (gameState.CurrentPlayer != player) return;
            
        //    // TODO: Check if chess piece 
        //    //if (board.turnOf != playingAs)
        //    //{
        //    //    return;
        //    //}

        //}

        //public void ShowLegalMoves()
        //{
        //    board.MarkLegalMoves(board.cellGrid[0, 1], Piece.PieceType.Knight);

        //    for (int i = 0; i < board.BoardSize; i++)
        //    {
        //        for (int j = 0; j < board.BoardSize; j++)
        //        {
        //            ImageButton square = (ImageButton)((FrameLayout)chessBoard.GetChildAt((i * 8) + j)).GetChildAt(0);

        //            if (board.cellGrid[i, j].legalNextMove) square.SetBackgroundColor(Resources.GetColor(Resource.Color.black, Theme));
        //            if (board.cellGrid[i, j].occupied) square.SetBackgroundColor(Resources.GetColor(Resource.Color.selectedYellow, Theme));

        //        }
        //    }
        //}

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
                    //pieceImages[r, c].Source = Images.GetImage(piece);

                    if (PieceImages.GetImage(piece) != 0) ib.SetImageDrawable(Resources.GetDrawable(PieceImages.GetImage(piece), Theme));
                    else ib.SetImageDrawable(null);
                }
            }

            if (player == Player.Black)
            {
                chessBoard.Rotation = 180;
            }
        }
    }
}