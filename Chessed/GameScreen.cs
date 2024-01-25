using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.Autofill;
using Android.Views;
using Android.Widget;
using Java.Security.Cert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using static Android.Gms.Common.Apis.Api;
using static Android.Provider.MediaStore;

namespace Chessed
{
    [Activity(Label = "GameScreen")]
    public class GameScreen : Activity, View.IOnClickListener
    {
        readonly string abc = "ABCDEFGH";
        GridLayout chessBoard;
        private GameState gameState;
        private Position selectedPos = null;

        //Board board;

        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        ClientWebSocket client = Client.Instance.client;

        const Player player = Player.White;


        //Piece.PieceColor playingAs = Piece.PieceColor.White;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.chess_board);

            //board = new Board();
            gameState = new GameState(Player.White, Board.Initial());

            chessBoard = FindViewById<GridLayout>(Resource.Id.chessBoard);

            BuildBoard();
            DrawBoard(gameState.Board);
            //FillBoard();

            //board.cellGrid[1, 1].occupied = true;
            //ShowLegalMoves();
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
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes);
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
                        var byteMessage = Encoding.UTF8.GetBytes($"{move.FromPos}-{move.ToPos}");
                        var segmnet = new ArraySegment<byte>(byteMessage);

                        await client.SendAsync(segmnet, WebSocketMessageType.Text, true, default);
                    });
                }

                if (move.Type == MoveType.PawnPromotion)
                {
                    //HandlePromotion(move.FromPos, move.ToPos);
                }
                else
                {
                    HandleMove(move);
                }
            }
        }

        private void HandleMove(Move move)
        {
            gameState.MakeMove(move);
            DrawBoard(gameState.Board);
            //SetCursor(gameState.CurrentPlayer);

            //if (gameState.IsGameOver())
            //{
            //    ShowGameOver();
            //}
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

        public void OnClick(View v)
        {
            if (gameState.CurrentPlayer != player) return;
            int[] sq = Board.PositionFromStr(v.TransitionName);
            Position pos = new Position(sq[0], sq[1]);

            

            if (selectedPos == null)
            {
                OnFromPositionSelected(pos);
            }
            else
            {
                OnToPositionSelected(pos);
            }
            // TODO: Check if chess piece 
            //if (board.turnOf != playingAs)
            //{
            //    return;
            //}

        }

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

                    TextView squareTv = new TextView(this)
                    {
                        LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
                        Gravity = GravityFlags.Left | GravityFlags.Bottom,
                    };

                    buttonView.SetBackgroundColor(Resources.GetColor((j + i) % 2 == 1 ? Resource.Color.board_dark : Resource.Color.board_light, Theme));

                    if (j != 0 && i == 7)
                    {
                        squareTv.Text = abc[j].ToString();

                    }

                    if (j == 0)
                    {
                        squareTv.Text = (8 - i).ToString();
                        if (i == 7)
                        {
                            squareTv.Text += "A";
                        }
                    }

                    squareTv.SetPadding(10, 10, 10, 10);

                    buttonView.TransitionName = $"{i}{j}";

                    buttonView.SetOnClickListener(this);

                    container.AddView(buttonView);
                    container.AddView(squareTv);

                    chessBoard.AddView(container);
                }
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
        }
        void FillBoard()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    ImageButton ib = (ImageButton)((FrameLayout)chessBoard.GetChildAt(8 * i + j)).GetChildAt(0);

                    int drawable = 0;
                    // PAWNS
                    if (i == 6)
                    {
                        drawable = Resource.Drawable.pw;
                    }
                    else if (i == 1) {

                        drawable = Resource.Drawable.pb;
                    }
                    else if (j == 0 || j == 7)
                    {
                        if (i == 0) drawable = Resource.Drawable.rb;
                        else if (i == 7) drawable = Resource.Drawable.rw;
                    }
                    else if (j == 1 || j == 6)
                    {
                        if (i == 0) drawable = Resource.Drawable.nb;
                        else if (i == 7) drawable = Resource.Drawable.nw;
                    }
                    else if (j == 2 || j == 5)
                    {
                        if (i == 0) drawable = Resource.Drawable.bb;
                        else if (i == 7) drawable = Resource.Drawable.bw;
                    }
                    else if (j == 3)
                    {
                        if (i == 0) drawable = Resource.Drawable.bq;
                        else if (i == 7) drawable = Resource.Drawable.wq;
                    }
                    else if (j == 4)
                    {
                        if (i == 0) drawable = Resource.Drawable.kb;
                        else if (i == 7) drawable = Resource.Drawable.kw;
                    }

                    if (drawable != 0) ib.SetImageDrawable(GetDrawable(drawable));


                }
            }
        }
    }
}