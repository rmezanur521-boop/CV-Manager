import requests
from odoo import _, fields, models
from odoo.exceptions import UserError


class CvPositionImportWizard(models.TransientModel):
    _name = 'cv.position.import.wizard'
    _description = 'Import CVPlatform Position Wizard'

    api_base_url = fields.Char(
        string='CVPlatform Base URL',
        required=True,
        default=lambda self: self._default_api_base_url(),
    )
    api_token = fields.Char(string='API Token', required=True)
    position_id = fields.Many2one('cv.position', string='Existing Position')

    def _default_api_base_url(self):
        return self.env['ir.config_parameter'].sudo().get_param(
            'cv_position_viewer.api_base_url',
            'https://host.docker.internal:7052',
        )

    def action_import(self):
        self.ensure_one()

        if not self.api_base_url or not self.api_token:
            raise UserError(_('Both Base URL and API Token are required.'))

        base_url = self.api_base_url.strip().rstrip('/')
        url = f'{base_url}/api/external/v1/position/aggregates'
        headers = {
            'Authorization': f'Bearer {self.api_token.strip()}',
            'X-Api-Token': self.api_token.strip(),
            'Accept': 'application/json',
        }

        try:
            response = requests.get(url, headers=headers, timeout=15, verify=False)
        except requests.exceptions.ConnectionError:
            raise UserError(_('Unable to connect to CVPlatform at %s.') % base_url)
        except requests.exceptions.Timeout:
            raise UserError(_('Connection to CVPlatform timed out.'))
        except requests.exceptions.RequestException as exc:
            raise UserError(_('Network error while connecting to CVPlatform: %s') % str(exc))

        if response.status_code == 401:
            raise UserError(_('Authentication failed. The API token is invalid or has been revoked.'))
        elif response.status_code == 404:
            raise UserError(_('Position not found on CVPlatform.'))
        elif response.status_code == 429:
            raise UserError(_('Rate limit exceeded. Please wait a minute before retrying.'))
        elif response.status_code != 200:
            raise UserError(_('CVPlatform API returned error: %s - %s') % (response.status_code, response.text))

        try:
            data = response.json()
        except ValueError:
            raise UserError(_('Failed to parse response from CVPlatform as JSON.'))

        attribute_commands = [(5, 0, 0)]
        for attr in data.get('attributes', []):
            breakdown_commands = []
            dropdown_data = attr.get('dropdown')
            if dropdown_data and dropdown_data.get('topOptions'):
                for opt in dropdown_data['topOptions']:
                    breakdown_commands.append((0, 0, {
                        'value': opt.get('value', ''),
                        'count': opt.get('count', 0),
                    }))

            text_data = attr.get('singleLineText')
            if text_data and text_data.get('topValues'):
                for tv in text_data['topValues']:
                    breakdown_commands.append((0, 0, {
                        'value': tv.get('value', ''),
                        'count': tv.get('count', 0),
                    }))

            num_data = attr.get('numeric') or {}
            date_data = attr.get('date') or {}
            dr_data = attr.get('dateRange') or {}
            bool_data = attr.get('boolean') or {}
            md_data = attr.get('markdownText') or {}

            line_vals = {
                'attribute_id': attr.get('attributeId', 0),
                'name': attr.get('name', ''),
                'attribute_type': attr.get('type', ''),
                'is_required': attr.get('isRequired', False),
                'response_count': attr.get('responseCount', 0),
                'summary': attr.get('summary', ''),
                'numeric_min': num_data.get('min', 0.0),
                'numeric_max': num_data.get('max', 0.0),
                'numeric_average': num_data.get('average', 0.0),
                'date_min': date_data.get('minDate') or False,
                'date_max': date_data.get('maxDate') or False,
                'date_range_earliest_start': dr_data.get('earliestStart') or False,
                'date_range_latest_end': dr_data.get('latestEnd') or False,
                'boolean_true_count': bool_data.get('trueCount', 0),
                'boolean_false_count': bool_data.get('falseCount', 0),
                'markdown_avg_length': md_data.get('averageLength', 0.0),
                'value_breakdown_ids': breakdown_commands,
            }
            attribute_commands.append((0, 0, line_vals))

        vals = {
            'external_id': data.get('id'),
            'title': data.get('title', ''),
            'company': data.get('company') or False,
            'level': data.get('level') or False,
            'short_description': data.get('shortDescription') or False,
            'published_cv_count': data.get('publishedCvCount', 0),
            'last_synced_at': fields.Datetime.now(),
            'api_base_url': base_url,
            'attribute_ids': attribute_commands,
        }

        position = self.env['cv.position'].sudo().search([('external_id', '=', data['id'])], limit=1)
        if position:
            position.sudo().write(vals)
        else:
            position = self.env['cv.position'].sudo().create(vals)

        return {
            'type': 'ir.actions.act_window',
            'name': position.title,
            'res_model': 'cv.position',
            'res_id': position.id,
            'view_mode': 'form',
            'target': 'current',
        }
