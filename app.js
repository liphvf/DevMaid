const inquirer = require("./lib/inquirer");

let program = require("commander");

program
    .command("list") // sub-command name
    .alias("ls") // alternative sub-command is `al`
    .description("List coffee menu") // command description

    // function to execute when command is uses
    .action(function() {
        console.log("listando aqui");
    });

program.parse(process.argv);

// const run = async () => {
//   const credentials = await inquirer.askGithubCredentials();
//   console.log(credentials);
// }

// run();
