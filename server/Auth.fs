module Auth

open System.Data
open Hopac
open MySql.Data.MySqlClient

type User =
    { firstName : string
      lastName : string
      email : string }

// Hard coded users for easy demoing here
let userExists email =
    job { 
        let users =
            [ { firstName = "Bob"
                lastName = "McBob"
                email = "bob@example.com" }
              { firstName = "Fred"
                lastName = "McFred"
                email = "fred@example.com" } ]
        return List.tryFind (fun u -> u.email = email) users
    }
// An example of looking up users in a MySQL database
// let connString =
//     let csb = MySqlConnectionStringBuilder()
//     csb.Server <- "localhost"
//     csb.Database <- "db"
//     csb.SslMode <- MySqlSslMode.None
//     csb.UserID <- "user"
//     csb.ConnectionString
// let getConn () =
//     new MySqlConnection(connString)
// let userExists (email : string) =
//     job {
//         use conn = getConn ()
//         do! conn.OpenAsync() |> Job.awaitUnitTask
//         use cmd = new MySqlCommand()
//         cmd.Connection <- conn
//         cmd.CommandText <- "select first_name, last_name from `users` where users.email = (@p)"
//         cmd.Parameters.AddWithValue("p", email) |> ignore
//         let! reader =
//             fun () -> cmd.ExecuteReaderAsync()
//             |> Job.fromTask
//         if reader.HasRows then
//             do! reader.ReadAsync() |> Job.awaitUnitTask
//             return
//                 { firstName = reader.GetString(0)
//                   lastName = reader.GetString(1)
//                   email = email }
//                 |> Some
//         else
//             return None
//     }
