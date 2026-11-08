# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

Edit the "Label in our tracker" column to match whatever vocabulary you actually use.

## Lifecycle states

The `Status:` line in an issue file also takes two lifecycle states. They are not triage roles: they record work on a ticket once it is picked up (see `issue-tracker.md`).

| Status    | Meaning                                                 |
| --------- | ------------------------------------------------------- |
| `claimed` | Someone has claimed the ticket and is working on it     |
| `done`    | The ticket is resolved; tickets it blocks are unblocked |

So the `Status:` values in use are the five triage roles above plus `claimed` and `done`.
