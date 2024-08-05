using System.Net;
using System.Net.Sockets;
using System.Text;

List<List<int>> combinations = new List<List<int>>()
{
    new List<int>() { 0, 1, 2 }, new List<int>() { 3, 4, 5 }, new List<int>() { 6, 7, 8 },
    new List<int>() { 0, 3, 6 }, new List<int>() { 1, 4, 7 }, new List<int>() { 2, 5, 8 },
    new List<int>() { 0, 4, 8 }, new List<int>() { 2, 4, 6 }
};

int Winner(bool?[] field, bool goal) // 2 - win, 1 - draw, 0 - nothing
{
    int woNulls = 0;
    foreach (List<int> combination in combinations)
    {
        List<bool?> cur = new List<bool?>();
        foreach (int i in combination)
            cur.Add(field[i]);
        if (!cur.Any(item => item == null))
        {
            woNulls++;
            if (cur.All(item => item == goal))
                return 2;
        }
    }
    if (woNulls == 8)
        return 1;
    return 0;
}

string FormResponse(bool?[] arr)
{
    string res = "";
    for (int i = 0; i < arr.Length; i++)
    {
        if (i % 3 == 0 && i > 0)
            res += '\n';
        if (arr[i] == null)
            res += '.';
        else if (arr[i] == true)
            res += 'X';
        else
            res += 'O';
    }
    res += '\n';
    return res;
}
void Print(string msg, bool turn)
{
    Console.Clear();
    Console.Write(msg);
    Console.Write(turn ? "\nВаш ход: " : "\nХод соперника\n");
}

void PrintEnd(int status)
{
    if (status == 2)
        Console.WriteLine("Вы победили!");
    else if (status == 1)
        Console.WriteLine("Ничья...");
    else
        Console.WriteLine("Вы проиграли!");
}

bool Continue()
{
    Console.WriteLine("\nПродолжить играть\nс этим игроком?\n1 - да, 2 - нет");
    ConsoleKeyInfo resp;
    do
    {
        resp = Console.ReadKey();
    } while (!resp.KeyChar.Equals('1') && !resp.KeyChar.Equals('2'));
    if (resp.KeyChar == '1') 
        return true;
    else 
        return false;
}

void Game(bool host, int port, string ip = "")
{
    bool game = true;
    byte[] data = new byte[16];
    if (host) // host
    {
        var tcpListener = new TcpListener(IPAddress.Any, port);
        Console.WriteLine("Ожидаем подключения...");
        bool?[] field = new bool?[9]; for (int i = 0; i < field.Length; i++) { field[i] = null; }
        int turn = 0; // 0 - host, 1 - client
        int bytes;
        string response;
        bool cont;
        try
        {
            tcpListener.Start();

            using (TcpClient tcpClient = tcpListener.AcceptTcpClient())
            {
                var stream = tcpClient.GetStream();
                while (game)
                {
                    if (turn == 0)
                    {
                        Print(FormResponse(field), true);

                        int val = int.Parse(Console.ReadLine()) - 1;
                        field[val] = true;

                        int status = Winner(field, true);

                        response = FormResponse(field);
                        data = Encoding.UTF8.GetBytes($"{response} {status}");
                        stream.Write(data);

                        Print(response, true);
                        if (status == 2)
                        {
                            PrintEnd(2);
                        }
                        else if (status == 1)
                        {
                            PrintEnd(1);
                        }
                        if (status != 0)
                        {
                            Console.WriteLine("Одидание ответа игрока...");
                            bytes = stream.Read(data);
                            response = Encoding.UTF8.GetString(data, 0, bytes);
                            if (response[^1] == '0')
                            {
                                Console.WriteLine("Игрок отключился");
                                break;
                            }
                            cont = Continue();
                            stream.Write(Encoding.UTF8.GetBytes($"{(cont ? 1 : 0)}"));
                            if (!cont)
                            {
                                game = false;
                                break;
                            }
                            else
                                for (int i = 0; i < field.Length; i++) { field[i] = null; }
                        }
                        turn = 1;
                        Print(response, false);
                    }
                    else
                    {
                        Print(FormResponse(field), false);

                        bytes = stream.Read(data);
                        response = Encoding.UTF8.GetString(data, 0, bytes);

                        int idx = int.Parse(response) - 1;
                        field[idx] = false;
                        int status = Winner(field, false);
                        response = FormResponse(field);
                        stream.Write(Encoding.UTF8.GetBytes($"{response} {status}"));

                        Print(response, false);
                        if (status == 2)
                        {
                            PrintEnd(0);
                        }
                        else if (status == 1)
                        {
                            PrintEnd(1);
                        }
                        if (status != 0)
                        {
                            Console.WriteLine("Одидание ответа игрока...");
                            bytes = stream.Read(data);
                            response = Encoding.UTF8.GetString(data, 0, bytes);
                            if (response[^1] == '0')
                            {
                                Console.WriteLine("Игрок отключился");
                                break;
                            }
                            cont = Continue();
                            stream.Write(Encoding.UTF8.GetBytes($"{(cont ? 1 : 0)}"));
                            if (!cont)
                            {
                                game = false;
                                break;
                            }
                            else
                                for (int i = 0; i < field.Length; i++) { field[i] = null; }
                        }
                        turn = 0;
                        Print(response, true);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally /////////////////////////////////////////////////////////////////
        {
            Console.WriteLine("Нажмите любую клавишу\nчтобы выйти...");
            Console.ReadKey();
            tcpListener.Stop();
        }
    }
    else // client
    {
        int turn = 0; // 0 - host, 1 - client
        string strField = "...\n...\n...\n";
        Print(strField, false);
        int bytes;
        string response;
        bool cont;
        using (TcpClient tcpClient = new TcpClient())
        {
            try
            {
                tcpClient.Connect(ip, port);
                var stream = tcpClient.GetStream();
                while (game)
                {
                    if (turn == 0)
                    {
                        Print(strField, false);
                        //var stream = tcpClient.GetStream();

                        bytes = stream.Read(data);
                        response = Encoding.UTF8.GetString(data, 0, bytes);
                        strField = "";
                        int i = 0;
                        while (response[i] != ' ')
                        {
                            strField += response[i];
                            i++;
                        }
                        Print(strField, false);
                        if (response[^1] == '2')
                        {
                            PrintEnd(0);
                        }
                        else if (response[^1] == '1')
                        {
                            PrintEnd(1);
                        }
                        if (response[^1] != '0')
                        {
                            cont = Continue();
                            stream.Write(Encoding.UTF8.GetBytes($"{(cont ? 1 : 0)}"));
                            if (!cont)
                            {
                                game = false;
                                break;
                            }
                            bytes = stream.Read(data);
                            response = Encoding.UTF8.GetString(data, 0, bytes);
                            if (response[^1] == '0')
                            {
                                Console.WriteLine("Игрок отключился");
                                game = false;
                                break;
                            }
                            else
                                strField = "...\n...\n...\n";
                        }
                        Print(strField, true);
                        turn = 1;
                    }
                    else
                    {
                        Print(strField, true);

                        string val = Console.ReadLine();

                        //var stream = tcpClient.GetStream();
                        stream.Write(Encoding.UTF8.GetBytes(val));

                        bytes = stream.Read(data);
                        response = Encoding.UTF8.GetString(data, 0, bytes);
                        strField = "";
                        int i = 0;
                        while (response[i] != ' ')
                        {
                            strField += response[i];
                            i++;
                        }
                        Print(strField, true);
                        if (response[^1] == '2')
                        {
                            PrintEnd(2);
                        }
                        else if (response[^1] == '1')
                        {
                            PrintEnd(1);
                        }
                        if (response[^1] != '0')
                        {
                            cont = Continue();
                            stream.Write(Encoding.UTF8.GetBytes($"{(cont ? 1 : 0)}"));
                            if (!cont)
                            {
                                game = false;
                                break;
                            }
                            bytes = stream.Read(data);
                            response = Encoding.UTF8.GetString(data, 0, bytes);
                            if (response[^1] == '0')
                            {
                                Console.WriteLine("Игрок отключился");
                                break;
                            }
                            else
                                strField = "...\n...\n...\n";
                        }
                        Print(strField, false);
                        turn = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally //////////////////////////////////////////////////////
            {
                tcpClient.Close();
                Console.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
            }
        }
    }
}

Console.WindowHeight = 15;
Console.WindowWidth = 25;

Console.Clear();
Console.Write("Быть хостом?\n1 - Нет\n2 - Да\nХост - X, нет - O\n");

ConsoleKeyInfo resp;
do
{
    resp = Console.ReadKey();
} while (!resp.KeyChar.Equals('1') && !resp.KeyChar.Equals('2'));

if (resp.Key == ConsoleKey.D1 || resp.Key == ConsoleKey.NumPad1) // client
{
    Console.WriteLine("\nУкажите ip-адрес хоста: ");
    string ip = Console.ReadLine();
    Console.WriteLine("Укажите порт хоста\nдля подключения: ");
    int port = int.Parse(Console.ReadLine());
    Game(false, port, ip);

}
else if (resp.Key == ConsoleKey.D2 || resp.Key == ConsoleKey.NumPad2) // host
{
    Console.WriteLine("\nУкажите порт хоста: ");
    int port = int.Parse(Console.ReadLine());
    Game(true, port);
}
