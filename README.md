# Chronos

Chronos is an application designed to simplify and automate the process of filling out Jira worklogs. It allows users to log their work time more efficiently and effortlessly.

![image](https://github.com/tolmachev-pravo/chronos/assets/62241382/009697f2-8e71-4473-87a2-0a82c72f6761)

# Features
* Seamless integration with Jira for authentication
* Automatic suggestions for creating worklogs based on your profile settings
* Customizable profile settings for personalized time management
* Different color codes for easy identification of suggested worklogs

# Getting Started
To use Chronos, you need to have a Jira account. Once you have an account, follow these steps:

1. Clone the repository to your local machine.
2. Configure the application settings in the appsettings.json file:
    * Set the Jira URL.
    * Specify the list of cached issues (optional).
    * Configure the Serilog settings (optional).
3. Build and run the application.
4. Customize your profile settings:
    * Set the date range for searching issues to calculate worklogs.
    * Specify your daily working start and end time.
    * Define the issue status for when you start working on it.
    * Set the default time for logging worklogs through comments.
    * Specify the average lunch break duration.


# Worklog Suggestions

Chronos provides a list of suggested worklogs categorized by days. Here's how to interpret the color codes:

* Purple: Suggested worklogs for tasks where you are the assignee.
* Green: Worklogs that are already logged in Jira. If no time is specified, they are associated with the suggested worklog above. If time is specified, they are independent worklogs.
* Blue: Suggested worklogs for tasks where you have left comments.

# Contributing
Contributions are welcome! If you have any suggestions, bug reports, or feature requests, please open an issue or submit a pull request.
